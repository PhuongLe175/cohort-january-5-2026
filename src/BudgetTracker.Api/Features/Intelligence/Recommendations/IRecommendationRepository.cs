namespace BudgetTracker.Api.Features.Intelligence.Recommendations;

public interface IRecommendationRepository
{
    Task<List<RecommendationDto>> GetActiveRecommendationsAsync(string userId);
    Task GenerateRecommendationsAsync(string userId);
}

public interface IRecommendationWorker
{
    Task ProcessAllUsersRecommendationsAsync();
    Task ProcessUserRecommendationsAsync(string userId);
}
