using System.Security.Claims;

namespace BudgetTracker.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal claims)
        => claims.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public static string GetUserEmail(this ClaimsPrincipal claims)
        => claims.FindFirstValue(ClaimTypes.Email)!;
}