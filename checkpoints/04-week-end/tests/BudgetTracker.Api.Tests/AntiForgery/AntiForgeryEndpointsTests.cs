using System.Net;
using BudgetTracker.Api.Auth;
using BudgetTracker.Api.Tests.Fixtures;

namespace BudgetTracker.Api.Tests.AntiForgery;

[Collection("Database")]
public class AntiForgeryEndpointsTests
{
    private readonly ApiFixture _fixture;
    private readonly HttpClient _client;

    public AntiForgeryEndpointsTests(ApiFixture databaseFixture)
    {
        _fixture = databaseFixture;
        _client = databaseFixture.CreateClient();
    }

    [Fact]
    public async Task Should_return_anti_forgery_token_when_authenticated()
    {
        var user = await _fixture.CreateTestUserAsync($"antiforgery_test_{Guid.NewGuid():N}@example.com");
        _fixture.AuthenticateClient(_client, user.Id, user.Email!);

        var response = await _client.GetAsync("/api/antiforgery/token", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        var xsrfCookie = cookies.FirstOrDefault(c => c.Contains("XSRF-TOKEN"));
        Assert.NotNull(xsrfCookie);
        Assert.Contains("XSRF-TOKEN=", xsrfCookie);
    }

    [Fact]
    public async Task Should_return_unauthorized_when_not_authenticated()
    {
        var response = await _client.GetAsync("/api/antiforgery/token", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Should_set_cookie_as_non_http_only()
    {
        var user = await _fixture.CreateTestUserAsync($"antiforgery_test_{Guid.NewGuid():N}@example.com");
        _fixture.AuthenticateClient(_client, user.Id, user.Email!);

        var response = await _client.GetAsync("/api/antiforgery/token", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        var xsrfCookie = cookies.FirstOrDefault(c => c.Contains("XSRF-TOKEN"));
        Assert.NotNull(xsrfCookie);
        Assert.DoesNotContain("HttpOnly", xsrfCookie);
    }

    [Fact]
    public async Task Should_generate_different_tokens_for_different_requests()
    {
        var user = await _fixture.CreateTestUserAsync($"antiforgery_test_{Guid.NewGuid():N}@example.com");
        _fixture.AuthenticateClient(_client, user.Id, user.Email!);

        var response1 = await _client.GetAsync("/api/antiforgery/token", TestContext.Current.CancellationToken);
        var response2 = await _client.GetAsync("/api/antiforgery/token", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var cookies1 = response1.Headers.GetValues("Set-Cookie").ToList();
        var cookies2 = response2.Headers.GetValues("Set-Cookie").ToList();

        var token1 = ExtractTokenFromCookie(cookies1);
        var token2 = ExtractTokenFromCookie(cookies2);

        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public async Task Should_return_valid_token_format()
    {
        var user = await _fixture.CreateTestUserAsync($"antiforgery_test_{Guid.NewGuid():N}@example.com");
        _fixture.AuthenticateClient(_client, user.Id, user.Email!);

        var response = await _client.GetAsync("/api/antiforgery/token", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        var token = ExtractTokenFromCookie(cookies);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.True(token.Length > 10);
    }

    private string? ExtractTokenFromCookie(List<string> cookies)
    {
        var xsrfCookie = cookies.FirstOrDefault(c => c.Contains("XSRF-TOKEN"));
        if (xsrfCookie == null) return null;

        var tokenPart = xsrfCookie.Split(';')[0];
        var tokenValue = tokenPart.Split('=')[1];
        return tokenValue;
    }

}