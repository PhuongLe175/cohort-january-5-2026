using BudgetTracker.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Features.Intelligence.Recommendations;

public class RecommendationBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<RecommendationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Recommendation background service started");

        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var worker = scope.ServiceProvider.GetRequiredService<IRecommendationWorker>();

                await worker.ProcessAllUsersRecommendationsAsync();
                await CleanupExpiredRecommendationsAsync(scope);

                var nextRun = GetNextRunTime();
                var delay = nextRun - DateTime.UtcNow;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in recommendation background service, retrying in 30 minutes");
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }

    private async Task CleanupExpiredRecommendationsAsync(IServiceScope scope)
    {
        var context = scope.ServiceProvider.GetRequiredService<BudgetTrackerContext>();
        var now = DateTime.UtcNow;

        var expired = await context.Recommendations
            .Where(r => r.Status == RecommendationStatus.Active
                && r.ExpiresAt.HasValue && r.ExpiresAt.Value <= now)
            .ToListAsync();

        foreach (var rec in expired)
            rec.Status = RecommendationStatus.Expired;

        var cutoff = now.AddDays(-30);
        var old = await context.Recommendations
            .Where(r => r.Status != RecommendationStatus.Active && r.CreatedAt < cutoff)
            .ToListAsync();

        context.Recommendations.RemoveRange(old);
        await context.SaveChangesAsync();

        if (expired.Count > 0 || old.Count > 0)
            logger.LogInformation("Cleaned up {Expired} expired and {Old} old recommendations", expired.Count, old.Count);
    }

    private static DateTime GetNextRunTime()
    {
        var now = DateTime.UtcNow;
        var next = now.Date.AddHours(6);
        if (next <= now)
            next = next.AddDays(1);
        return next;
    }
}
