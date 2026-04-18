using Microsoft.AspNetCore.Antiforgery;

namespace BudgetTracker.Api.AntiForgery;

public class ConditionalAntiforgeryFilter : IEndpointFilter
{
    private readonly IAntiforgery _antiforgery;

    public ConditionalAntiforgeryFilter(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        
        if (IsApiKeyAuthenticatedRequest(httpContext))
        {
            httpContext.Features.Set<IAntiforgeryValidationFeature>(new DisabledAntiforgeryValidationFeature());
        }
        else
        {
            // For cookie-authenticated requests, validate the anti-forgery token
            try
            {
                await _antiforgery.ValidateRequestAsync(httpContext);
            }
            catch (AntiforgeryValidationException ex)
            {
                return Results.BadRequest($"Invalid anti-forgery token: {ex.Message}");
            }
        }

        return await next(context);
    }

    private static bool IsApiKeyAuthenticatedRequest(HttpContext httpContext) 
        => httpContext.Request.Headers.ContainsKey("X-API-Key");
}

public class DisabledAntiforgeryValidationFeature : IAntiforgeryValidationFeature
{
    public bool IsValid => true;
    public Exception? Error => null;
}

