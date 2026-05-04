using System.Text.Json;
using BudgetTracker.Api.Infrastructure;
using BudgetTracker.Api.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace BudgetTracker.Api.Features.Intelligence.Recommendations;

public class RecommendationAgent(
    BudgetTrackerContext context,
    IChatClient chatClient,
    ILogger<RecommendationAgent> logger) : IRecommendationRepository
{
    public async Task<List<RecommendationDto>> GetActiveRecommendationsAsync(string userId)
    {
        var now = DateTime.UtcNow;
        return await context.Recommendations
            .Where(r => r.UserId == userId
                && r.Status == RecommendationStatus.Active
                && (r.ExpiresAt == null || r.ExpiresAt > now))
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => r.MapToDto())
            .ToListAsync();
    }

    public async Task GenerateRecommendationsAsync(string userId)
    {
        var latestImport = await context.Transactions
            .Where(t => t.UserId == userId)
            .MaxAsync(t => (DateTime?)t.ImportedAt);

        if (latestImport == null)
        {
            logger.LogDebug("No transactions found for user {UserId}, skipping recommendations", userId);
            return;
        }

        var lastGenerated = await context.Recommendations
            .Where(r => r.UserId == userId)
            .MaxAsync(r => (DateTime?)r.CreatedAt);

        if (lastGenerated.HasValue && lastGenerated.Value > latestImport.Value)
        {
            logger.LogDebug("No new transactions since last recommendation generation for user {UserId}", userId);
            return;
        }

        var transactionCount = await context.Transactions
            .Where(t => t.UserId == userId)
            .CountAsync();

        if (transactionCount < 5)
        {
            logger.LogDebug("Not enough transactions ({Count}) for user {UserId}", transactionCount, userId);
            return;
        }

        var stats = await GetBasicStatsAsync(userId);
        var recommendations = await GenerateSimpleRecommendationsAsync(userId, stats);

        if (recommendations.Count > 0)
            await StoreRecommendationsAsync(userId, recommendations);
    }

    private async Task<BasicStats> GetBasicStatsAsync(string userId)
    {
        var transactions = await context.Transactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Date)
            .Take(1000)
            .ToListAsync();

        var income = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
        var expenses = Math.Abs(transactions.Where(t => t.Amount < 0).Sum(t => t.Amount));

        var categories = transactions
            .Where(t => t.Amount < 0 && !string.IsNullOrEmpty(t.Category))
            .GroupBy(t => t.Category!)
            .Select(g => new { Category = g.Key, Total = Math.Abs(g.Sum(t => t.Amount)), Count = g.Count() })
            .OrderByDescending(c => c.Total)
            .Take(10)
            .ToDictionary(c => c.Category, c => c.Total);

        var monthlyAverageExpenses = transactions.Count > 0
            ? expenses / Math.Max(1, (decimal)(transactions.Max(t => t.Date) - transactions.Min(t => t.Date)).TotalDays / 30)
            : 0;

        return new BasicStats(income, expenses, monthlyAverageExpenses, categories, transactions.Count);
    }

    private async Task<List<GeneratedRecommendation>> GenerateSimpleRecommendationsAsync(string userId, BasicStats stats)
    {
        var systemPrompt = """
            You are a financial advisor analyzing a user's spending patterns.
            Generate 3-5 actionable, specific financial recommendations based on their data.

            Respond with JSON in a markdown code block:
            ```json
            {
              "recommendations": [
                {
                  "title": "Short title (max 200 chars)",
                  "description": "Detailed actionable advice (max 1000 chars)",
                  "type": "SpendingAlert|SavingsOpportunity|BudgetTip|TrendInsight|CategoryOptimization",
                  "priority": "Low|Medium|High",
                  "amount": null or decimal,
                  "category": null or "category name"
                }
              ]
            }
            ```
            """;

        var topCategories = string.Join(", ", stats.TopCategories.Select(c => $"{c.Key}: €{c.Value:F2}"));

        var userPrompt = $"""
            User financial summary ({stats.TransactionCount} transactions analyzed):
            - Total income: €{stats.TotalIncome:F2}
            - Total expenses: €{stats.TotalExpenses:F2}
            - Net: €{(stats.TotalIncome - stats.TotalExpenses):F2}
            - Estimated monthly expenses: €{stats.MonthlyAverageExpenses:F2}
            - Top spending categories: {topCategories}

            Generate personalized financial recommendations for this user.
            """;

        try
        {
            var response = await chatClient.GetResponseAsync([
                new ChatMessage(ChatRole.System, systemPrompt),
                new ChatMessage(ChatRole.User, userPrompt)
            ]);

            var content = response.Text ?? string.Empty;
            return ParseRecommendations(content);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate recommendations for user {UserId}", userId);
            return [];
        }
    }

    private List<GeneratedRecommendation> ParseRecommendations(string content)
    {
        try
        {
            var json = content.ExtractJsonFromCodeBlock();
            var result = JsonSerializer.Deserialize<RecommendationResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.Recommendations ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse recommendation response");
            return [];
        }
    }

    private async Task StoreRecommendationsAsync(string userId, List<GeneratedRecommendation> generated)
    {
        var activeRecs = await context.Recommendations
            .Where(r => r.UserId == userId && r.Status == RecommendationStatus.Active)
            .ToListAsync();

        foreach (var rec in activeRecs)
            rec.Status = RecommendationStatus.Expired;

        var now = DateTime.UtcNow;
        var newRecs = generated.Select(g => new Recommendation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = g.Title,
            Description = g.Description,
            Type = Enum.TryParse<RecommendationType>(g.Type, out var type) ? type : RecommendationType.BudgetTip,
            Priority = Enum.TryParse<RecommendationPriority>(g.Priority, out var priority) ? priority : RecommendationPriority.Medium,
            Amount = g.Amount,
            Category = g.Category,
            Status = RecommendationStatus.Active,
            CreatedAt = now,
            ExpiresAt = now.AddDays(7)
        }).ToList();

        context.Recommendations.AddRange(newRecs);
        await context.SaveChangesAsync();

        logger.LogInformation("Stored {Count} recommendations for user {UserId}", newRecs.Count, userId);
    }

    private record BasicStats(
        decimal TotalIncome,
        decimal TotalExpenses,
        decimal MonthlyAverageExpenses,
        Dictionary<string, decimal> TopCategories,
        int TransactionCount);

    private class RecommendationResponse
    {
        public List<GeneratedRecommendation> Recommendations { get; set; } = [];
    }

    private class GeneratedRecommendation
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string? Category { get; set; }
    }
}
