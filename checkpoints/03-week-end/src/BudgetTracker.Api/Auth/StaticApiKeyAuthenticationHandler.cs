using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BudgetTracker.Api.Auth;

public class StaticApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public string ApiKeyHeaderName { get; set; } = "X-API-Key";
    public Dictionary<string, string> ValidApiKeys { get; set; } = new();
}

public class StaticApiKeyAuthenticationHandler : AuthenticationHandler<StaticApiKeyAuthenticationSchemeOptions>
{
    public StaticApiKeyAuthenticationHandler(
        IOptionsMonitor<StaticApiKeyAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(Options.ApiKeyHeaderName))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var apiKey = Request.Headers[Options.ApiKeyHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Check if the API key exists in our configured keys
        if (!Options.ValidApiKeys.ContainsKey(apiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        // Get the user ID associated with this API key
        var userId = Options.ValidApiKeys[apiKey];

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, $"ApiKey-{userId}"),
            new Claim("ApiKey", apiKey)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class StaticApiKeysConfiguration
{
    public const string SectionName = "StaticApiKeys";

    public Dictionary<string, ApiKeyInfo> Keys { get; set; } = new();
}

public class ApiKeyInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
