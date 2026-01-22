using System.Net;
using System.Net.Http.Headers;
using System.Text;
using BudgetTracker.Api.Features.Transactions.Import;
using BudgetTracker.Api.Tests.Extensions;
using BudgetTracker.Api.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

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

    [Fact]
    public async Task Should_import_valid_csv_and_store_transactions()
    {
        var email = "import_valid_csv_test@example.com";
        var user = await _fixture.CreateTestUserAsync(email);
        _fixture.AuthenticateClient(_client, user.Id, user.Email!);

        var csvContent = """
            Date,Description,Amount,Balance
            2025-01-15,Amazon Purchase,-45.67,1250.33
            2025-01-16,Coffee Shop,-5.89,1244.44
            2025-01-17,Salary Deposit,2500.00,3744.44
            """;

        // Add X-API-Key header to bypass anti-forgery validation in test
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-key");

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Checking Account"), "account");

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "test.csv");

        var response = await _client.PostAsync("/api/transactions/import", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.ToAsync<ImportResult>();
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalRows);
        Assert.Equal(3, result.ImportedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Errors);

        // Verify transactions are stored in database
        await using var db = _fixture.CreateBudgetTrackerDbContext();
        var storedTransactions = await db.Transactions
            .Where(t => t.UserId == user.Id && t.Account == "Checking Account")
            .ToListAsync();

        Assert.Equal(3, storedTransactions.Count);
    }
}
