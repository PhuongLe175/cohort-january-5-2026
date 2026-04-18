using Microsoft.AspNetCore.Antiforgery;

namespace BudgetTracker.Api.AntiForgery;

public static class AntiForgeryApi
{
    public static IEndpointRouteBuilder MapAntiForgeryEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/antiforgery/token", (IAntiforgery forgeryService, HttpContext context) =>
        {
            var tokens = forgeryService.GetAndStoreTokens(context);
            context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
                new CookieOptions { HttpOnly = false });

            return Results.Ok();
        }).RequireAuthorization();

        return routes;
    }
}
