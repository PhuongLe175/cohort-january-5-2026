using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace BudgetTracker.Api.Auth;

public static class AuthApi
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var usersApi = routes.MapGroup("/users");
        
        usersApi.MapIdentityApi<ApplicationUser>();
        
        usersApi.MapGet("/me", (ClaimsPrincipal claimsPrincipal) => 
                !claimsPrincipal.Claims.Any() ? Results.Unauthorized() : Results.Ok(new UserInfoDto { UserId = claimsPrincipal.GetUserId(), Email = claimsPrincipal.GetUserEmail() }))
            .WithName("GetUser")
            .WithSummary("Get user information")
            .WithDescription("Retrieves the current user's information")
            .Produces<UserInfoDto>()
            .RequireAuthorization();

        usersApi.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Ok();
        });

        return routes;
    }
}
