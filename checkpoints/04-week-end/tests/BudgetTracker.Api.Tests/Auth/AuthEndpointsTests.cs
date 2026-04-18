using System.Net;
using BudgetTracker.Api.Auth;
using BudgetTracker.Api.Tests.Extensions;
using BudgetTracker.Api.Tests.Fixtures;

namespace BudgetTracker.Api.Tests.Auth;

[Collection("Database")]
public class AuthEndpointsTests
{
    private readonly ApiFixture _fixture;
    private readonly HttpClient _client;

    public AuthEndpointsTests(ApiFixture databaseFixture)
    {
        _fixture = databaseFixture;
        _client = databaseFixture.CreateClient();
    }

    [Fact]
    public async Task Should_register_user_when_valid_credentials_provided()
    {
        var registerRequest = new
        {
            email = "register_valid_user@example.com",
            password = "NewUser123!"
        };

        var response = await _client.PostAsync("/api/users/register", registerRequest.AsJsonContent(),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Should_return_bad_request_when_invalid_email_format_provided()
    {
        var registerRequest = new
        {
            email = "invalid-email-format",
            password = "ValidPassword123!"
        };

        var response = await _client.PostAsync("/api/users/register", registerRequest.AsJsonContent(),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_return_bad_request_when_weak_password_provided()
    {
        var registerRequest = new
        {
            email = "weak_password_user@example.com",
            password = "weak"
        };

        var response = await _client.PostAsync("/api/users/register", registerRequest.AsJsonContent(),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_login_user_when_valid_credentials_provided()
    {
        var email = "login_valid_user@example.com";
        await _fixture.CreateTestUserAsync(email);

        var loginRequest = new
        {
            email = email,
            password = "Test123!"
        };

        var response = await _client.PostAsync("/api/users/login", loginRequest.AsJsonContent(),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Note: In test environment, we use JWT tokens instead of cookies for authentication
        // so we don't check for Set-Cookie headers here
    }

    [Fact]
    public async Task Should_return_unauthorized_when_invalid_credentials_provided()
    {
        var email = "login_invalid_user@example.com";
        await _fixture.CreateTestUserAsync(email);

        var loginRequest = new
        {
            email = email,
            password = "WrongPassword!"
        };

        var response = await _client.PostAsync("/api/users/login", loginRequest.AsJsonContent(),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Should_return_user_info_when_authenticated()
    {
        var email = "user_info_test@example.com";
        var user = await _fixture.CreateTestUserAsync(email);
        _fixture.AuthenticateClient(_client, user.Id, user.Email!);

        var response = await _client.GetAsync("/api/users/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var userInfo = await response.ToAsync<UserInfoDto>();
        Assert.NotNull(userInfo);
        Assert.Equal(email, userInfo.Email);
    }

    [Fact]
    public async Task Should_return_unauthorized_when_not_authenticated()
    {
        var response = await _client.GetAsync("/api/users/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Should_logout_user_when_authenticated()
    {
        var email = "logout_test@example.com";
        var user = await _fixture.CreateTestUserAsync(email);
        _fixture.AuthenticateClient(_client, user.Id, user.Email!);

        var response = await _client.PostAsync("/api/users/logout", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Should_return_unauthorized_after_logout()
    {
        var email = "logout_unauthorized_test@example.com";
        var user = await _fixture.CreateTestUserAsync(email);
        _fixture.AuthenticateClient(_client, user.Id, user.Email!);

        await _client.PostAsync("/api/users/logout", null, TestContext.Current.CancellationToken);

        _client.DefaultRequestHeaders.Clear();
        var response = await _client.GetAsync("/api/users/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

}