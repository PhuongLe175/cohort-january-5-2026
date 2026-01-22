using System.Net;
using BudgetTracker.Api.Features.Transactions.List;
using BudgetTracker.Api.Tests.Extensions;
using BudgetTracker.Api.Tests.Fixtures;

namespace BudgetTracker.Api.Tests.Features.Transactions;

[Collection("Database")]
public class TransactionListApiTests
{
    private readonly ApiFixture _fixture;
    private readonly HttpClient _client;

    public TransactionListApiTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task Should_return_empty_paged_result_when_no_transactions()
    {
        var email = "list_empty_test@example.com";
        var user = await _fixture.CreateTestUserAsync(email);
        _fixture.AuthenticateClient(_client, user.Id, user.Email!);

        var response = await _client.GetAsync("/api/transactions", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.ToAsync<PagedResult<object>>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task Should_return_unauthorized_when_not_authenticated()
    {
        var unauthenticatedClient = _fixture.CreateClient();

        var response = await unauthenticatedClient.GetAsync("/api/transactions", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
