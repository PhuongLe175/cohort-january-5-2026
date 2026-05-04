using BudgetTracker.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Features.Intelligence.Recommendations;

public class RecommendationProcessor(
    BudgetTrackerContext context,
    IRecommendationRepository repository,
    ILogger<RecommendationProcessor> logger) : IRecommendationWorker
{
    public async Task ProcessAllUsersRecommendationsAsync()
    {
        var userIds = await context.Transactions
            .Select(t => t.UserId)
            .Distinct()
            .ToListAsync();

        logger.LogInformation("Processing recommendations for {Count} users", userIds.Count);

        foreach (var userId in userIds)
        {
            await ProcessUserRecommendationsAsync(userId);
            await Task.Delay(100);
        }
    }

    public async Task ProcessUserRecommendationsAsync(string userId)
    {
        try
        {
            await repository.GenerateRecommendationsAsync(userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process recommendations for user {UserId}", userId);
        }
    }
}
