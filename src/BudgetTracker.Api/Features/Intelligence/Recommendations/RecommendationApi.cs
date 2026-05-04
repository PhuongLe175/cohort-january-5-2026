using System.Security.Claims;
using BudgetTracker.Api.Auth;

namespace BudgetTracker.Api.Features.Intelligence.Recommendations;

public static class RecommendationApi
{
    public static IEndpointRouteBuilder MapRecommendationEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/recommendations")
            .WithTags("Recommendations")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/", async (
            IRecommendationRepository repository,
            ClaimsPrincipal claimsPrincipal) =>
        {
            var userId = claimsPrincipal.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            var recommendations = await repository.GetActiveRecommendationsAsync(userId);
            return Results.Ok(recommendations);
        })
        .WithName("GetRecommendations")
        .WithSummary("Get active financial recommendations")
        .Produces<List<RecommendationDto>>()
        .ProducesProblem(401);

        return routes;
    }
}
