using BudgetTracker.Api.Features.Intelligence.Query;
using BudgetTracker.Api.Features.Intelligence.Recommendations;

namespace BudgetTracker.Api.Features.Intelligence;

public static class IntelligenceEndpoints
{
    public static IEndpointRouteBuilder MapIntelligenceEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapQueryEndpoints();
        routes.MapRecommendationEndpoints();
        return routes;
    }
}
