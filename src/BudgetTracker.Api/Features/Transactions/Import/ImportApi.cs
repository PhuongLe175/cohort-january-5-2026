using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BudgetTracker.Api.Auth;
using BudgetTracker.Api.Infrastructure;
using BudgetTracker.Api.Features.Transactions.Import.Detection;
using BudgetTracker.Api.Features.Transactions.Import.Enhancement;
using BudgetTracker.Api.Features.Transactions.Import.Processing;
using BudgetTracker.Api.AntiForgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Features.Transactions.Import;

public static class ImportApi
{
    public static IEndpointRouteBuilder MapTransactionImportEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/import", ImportAsync)
            .DisableAntiforgery()
            .AddEndpointFilter<ConditionalAntiforgeryFilter>();

        routes.MapPost("/import/enhance", EnhanceImportAsync)
            .AddEndpointFilter<ConditionalAntiforgeryFilter>();

        return routes;
    }

    private static async Task<Results<Ok<ImportResult>, BadRequest<string>>> ImportAsync(
        IFormFile file,
        [FromForm] string account,
        CsvImporter csvImporter,
        IImageImporter imageImporter,
        BudgetTrackerContext context,
        ITransactionEnhancer enhancementService,
        ClaimsPrincipal claimsPrincipal,
        ICsvStructureDetector detectionService)
    {
        var validationResult = ValidateFileInput(file);
        if (validationResult != null)
            return validationResult;

        if (string.IsNullOrWhiteSpace(account))
            return TypedResults.BadRequest("Account name is required");

        try
        {
            var userId = claimsPrincipal.GetUserId();
            var sessionHash = GenerateSessionHash(file.FileName, DateTime.UtcNow);

            await using var stream = file.OpenReadStream();

            var (result, transactions, detectionResult) = await ProcessFileAsync(
                stream, file.FileName, userId, account, csvImporter, imageImporter, detectionService);

            if (detectionResult != null && detectionResult.ConfidenceScore < 85)
            {
                return TypedResults.BadRequest(
                    $"Could not reliably detect CSV structure (confidence: {detectionResult.ConfidenceScore:F0}%). " +
                    (detectionResult.ErrorMessage ?? "Please ensure the file contains Date, Description, and Amount columns."));
            }

            if (detectionResult != null)
            {
                result.DetectionMethod = detectionResult.Method.ToString();
                result.DetectionConfidence = detectionResult.ConfidenceScore;
            }

            if (transactions.Any())
            {
                var descriptions = transactions.Select(t => t.Description).ToList();
                var enhancements = await enhancementService.EnhanceDescriptionsAsync(descriptions, account, userId, sessionHash);

                var enhancementResults = new List<TransactionEnhancementResult>();

                for (var i = 0; i < transactions.Count; i++)
                {
                    var transaction = transactions[i];
                    var enhancement = enhancements.FirstOrDefault(e =>
                        e.OriginalDescription == transaction.Description) ?? enhancements[i];

                    transaction.ImportSessionHash = sessionHash;

                    enhancementResults.Add(new TransactionEnhancementResult
                    {
                        TransactionId = transaction.Id,
                        ImportSessionHash = sessionHash,
                        TransactionIndex = i,
                        OriginalDescription = enhancement.OriginalDescription,
                        EnhancedDescription = enhancement.EnhancedDescription,
                        SuggestedCategory = enhancement.SuggestedCategory,
                        ConfidenceScore = enhancement.ConfidenceScore
                    });
                }

                await context.Transactions.AddRangeAsync(transactions);
                await context.SaveChangesAsync();

                result.ImportSessionHash = sessionHash;
                result.Enhancements = enhancementResults;
            }

            return TypedResults.Ok(result);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"Import failed: {ex.Message}");
        }
    }

    private static async Task<(ImportResult, List<Transaction>, CsvStructureDetectionResult?)> ProcessFileAsync(
        Stream stream, string fileName, string userId, string account,
        CsvImporter csvImporter, IImageImporter imageImporter, ICsvStructureDetector detectionService)
    {
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        return fileExtension switch
        {
            ".csv" => await ProcessCsvFileAsync(stream, fileName, userId, account, csvImporter, detectionService),
            ".png" or ".jpg" or ".jpeg" => await ProcessImageFileAsync(stream, fileName, userId, account, imageImporter),
            _ => throw new InvalidOperationException("Unsupported file type")
        };
    }

    private static async Task<(ImportResult, List<Transaction>, CsvStructureDetectionResult?)> ProcessCsvFileAsync(
        Stream stream, string fileName, string userId, string account,
        CsvImporter csvImporter, ICsvStructureDetector detectionService)
    {
        var detectionResult = await detectionService.DetectStructureAsync(stream);
        stream.Position = 0;
        var (result, transactions) = await csvImporter.ParseCsvAsync(stream, fileName, userId, account, detectionResult);
        return (result, transactions, detectionResult);
    }

    private static async Task<(ImportResult, List<Transaction>, CsvStructureDetectionResult?)> ProcessImageFileAsync(
        Stream stream, string fileName, string userId, string account,
        IImageImporter imageImporter)
    {
        var (result, transactions) = await imageImporter.ProcessImageAsync(stream, fileName, userId, account);
        return (result, transactions, null);
    }

    private static async Task<Results<Ok<EnhanceImportResult>, BadRequest<string>>> EnhanceImportAsync(
        [FromBody] EnhanceImportRequest request,
        BudgetTrackerContext context,
        ClaimsPrincipal claimsPrincipal)
    {
        try
        {
            var userId = claimsPrincipal.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return TypedResults.BadRequest("User not authenticated");

            var enhancedCount = 0;

            if (request.ApplyEnhancements)
            {
                var transactions = await context.Transactions
                    .Where(t => t.UserId == userId && t.ImportSessionHash == request.ImportSessionHash)
                    .ToListAsync();

                foreach (var enhancement in request.Enhancements)
                {
                    if (enhancement.ConfidenceScore < request.MinConfidenceScore)
                        continue;

                    var transaction = transactions.FirstOrDefault(t => t.Id == enhancement.TransactionId);
                    if (transaction == null)
                        continue;

                    transaction.Description = enhancement.EnhancedDescription;

                    if (!string.IsNullOrEmpty(enhancement.SuggestedCategory))
                        transaction.Category = enhancement.SuggestedCategory;

                    enhancedCount++;
                }

                if (enhancedCount > 0)
                    await context.SaveChangesAsync();
            }

            return TypedResults.Ok(new EnhanceImportResult
            {
                ImportSessionHash = request.ImportSessionHash,
                TotalTransactions = request.Enhancements.Count,
                EnhancedCount = enhancedCount,
                SkippedCount = request.Enhancements.Count - enhancedCount
            });
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"Enhancement failed: {ex.Message}");
        }
    }

    private static string GenerateSessionHash(string fileName, DateTime timestamp)
    {
        var input = $"{fileName}_{timestamp:yyyyMMddHHmmss}_{Guid.NewGuid()}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash)[..12];
    }

    private static BadRequest<string>? ValidateFileInput(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return TypedResults.BadRequest("Please select a valid file.");

        const int maxFileSize = 10 * 1024 * 1024; // 10MB
        if (file.Length > maxFileSize)
            return TypedResults.BadRequest("File size must be less than 10MB.");

        var allowedExtensions = new[] { ".csv", ".png", ".jpg", ".jpeg" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(fileExtension))
            return TypedResults.BadRequest("Only CSV files and images (PNG, JPG, JPEG) are supported.");

        return null;
    }
}
