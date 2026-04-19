using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BudgetTracker.Api.Auth;
using BudgetTracker.Api.Infrastructure;
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
        IFormFile file, [FromForm] string account,
        CsvImporter csvImporter, ITransactionEnhancer enhancer,
        BudgetTrackerContext context, ClaimsPrincipal claimsPrincipal)
    {
        var validationResult = ValidateFileInput(file, account);
        if (validationResult != null)
            return validationResult;

        try
        {
            var userId = claimsPrincipal.GetUserId();

            using var stream = file.OpenReadStream();
            var (result, transactions) = await csvImporter.ParseCsvAsync(stream, file.FileName, userId, account);

            var sessionHash = GenerateSessionHash(userId, file.FileName);
            result.ImportSessionHash = sessionHash;

            if (transactions.Any())
            {
                var rawDescriptions = transactions.Select(t => t.Description).ToList();
                var enhancements = await enhancer.EnhanceDescriptionsAsync(rawDescriptions);

                var enhancementResults = new List<TransactionEnhancementResult>();
                for (var i = 0; i < transactions.Count; i++)
                {
                    transactions[i].ImportSessionHash = sessionHash;
                    var enhancement = enhancements[i];
                    enhancementResults.Add(new TransactionEnhancementResult
                    {
                        TransactionId = transactions[i].Id,
                        OriginalDescription = enhancement.OriginalDescription,
                        CleanDescription = enhancement.CleanDescription,
                        SuggestedCategory = enhancement.SuggestedCategory
                    });
                }

                result.Enhancements = enhancementResults;

                await context.Transactions.AddRangeAsync(transactions);
                await context.SaveChangesAsync();
            }

            return TypedResults.Ok(result);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"Import failed: {ex.Message}");
        }
    }

    private static async Task<Results<Ok<EnhanceImportResult>, BadRequest<string>, NotFound>> EnhanceImportAsync(
        EnhanceImportRequest request, ITransactionEnhancer enhancer,
        BudgetTrackerContext context, ClaimsPrincipal claimsPrincipal)
    {
        if (string.IsNullOrWhiteSpace(request.ImportSessionHash))
            return TypedResults.BadRequest("ImportSessionHash is required");

        var userId = claimsPrincipal.GetUserId();

        var transactions = await context.Transactions
            .Where(t => t.UserId == userId && t.ImportSessionHash == request.ImportSessionHash)
            .ToListAsync();

        if (transactions.Count == 0)
            return TypedResults.NotFound();

        var rawDescriptions = transactions.Select(t => t.Description).ToList();
        var enhancements = await enhancer.EnhanceDescriptionsAsync(rawDescriptions);

        for (var i = 0; i < transactions.Count; i++)
        {
            transactions[i].Description = enhancements[i].CleanDescription;
            transactions[i].Category = enhancements[i].SuggestedCategory;
        }

        await context.SaveChangesAsync();

        return TypedResults.Ok(new EnhanceImportResult
        {
            UpdatedCount = transactions.Count,
            ImportSessionHash = request.ImportSessionHash
        });
    }

    private static string GenerateSessionHash(string userId, string fileName)
    {
        var input = $"{userId}:{fileName}:{DateTime.UtcNow:O}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash)[..12].ToLowerInvariant();
    }

    private static BadRequest<string>? ValidateFileInput(IFormFile file, string account)
    {
        if (file == null || file.Length == 0)
            return TypedResults.BadRequest("No file uploaded");

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return TypedResults.BadRequest("Only CSV files are supported");

        if (file.Length > 10 * 1024 * 1024)
            return TypedResults.BadRequest("File size exceeds 10MB limit");

        if (string.IsNullOrWhiteSpace(account))
            return TypedResults.BadRequest("Account name is required");

        return null;
    }
}
