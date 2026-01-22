using System.Net;
using System.Net.Http.Headers;
using System.Text;
using BudgetTracker.Api.Tests.Fixtures;

namespace BudgetTracker.Api.Tests.Features.Transactions;

[Collection("Database")]
public class TransactionImportApiTests
{
    private readonly ApiFixture _fixture;
    private readonly HttpClient _client;

    public TransactionImportApiTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task Should_return_bad_request_when_no_file_uploaded()
    {
        var email = "import_no_file_test@example.com";
        var user = await _fixture.CreateTestUserAsync(email);
        _fixture.AuthenticateClient(_client, user.Id, user.Email!);

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Checking"), "account");

        var response = await _client.PostAsync("/api/transactions/import", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_return_bad_request_when_non_csv_file_uploaded()
    {
        var email = "import_non_csv_test@example.com";
        var user = await _fixture.CreateTestUserAsync(email);
        _fixture.AuthenticateClient(_client, user.Id, user.Email!);

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Checking"), "account");

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("test content"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "test.txt");

        var response = await _client.PostAsync("/api/transactions/import", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_return_bad_request_when_account_missing()
    {
        var email = "import_no_account_test@example.com";
        var user = await _fixture.CreateTestUserAsync(email);
        _fixture.AuthenticateClient(_client, user.Id, user.Email!);

        using var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("date,description,amount\n2024-01-01,Test,100"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "test.csv");

        var response = await _client.PostAsync("/api/transactions/import", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
