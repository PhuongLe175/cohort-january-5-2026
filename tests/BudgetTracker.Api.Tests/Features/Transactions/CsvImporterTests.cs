using System.Text;
using BudgetTracker.Api.Features.Transactions.Import.Processing;

namespace BudgetTracker.Api.Tests.Features.Transactions;

public class CsvImporterTests
{
    private readonly CsvImporter _csvImporter = new();

    [Fact]
    public async Task Should_parse_valid_csv_with_standard_columns()
    {
        var csv = """
            Date,Description,Amount,Balance
            2025-01-15,Amazon Purchase,-45.67,1250.33
            2025-01-16,Coffee Shop,-5.89,1244.44
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var (result, transactions) = await _csvImporter.ParseCsvAsync(stream, "test.csv", "user123", "Checking");

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Errors);
        Assert.Equal(2, transactions.Count);
        Assert.Equal("Amazon Purchase", transactions[0].Description);
        Assert.Equal(-45.67m, transactions[0].Amount);
        Assert.Equal("Checking", transactions[0].Account);
        Assert.Equal("user123", transactions[0].UserId);
    }

    [Fact]
    public async Task Should_handle_alternative_column_names()
    {
        var csv = """
            Transaction Date,Memo,Transaction Amount,Running Balance
            2025-01-15,Grocery Store,-89.45,500.00
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var (result, transactions) = await _csvImporter.ParseCsvAsync(stream, "test.csv", "user123", "Savings");

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal("Grocery Store", transactions[0].Description);
        Assert.Equal(-89.45m, transactions[0].Amount);
        Assert.Equal(500.00m, transactions[0].Balance);
    }

    [Fact]
    public async Task Should_report_errors_for_invalid_rows()
    {
        var csv = """
            Date,Description,Amount,Balance
            2025-01-15,Valid Transaction,-45.67,1250.33
            invalid-date,Bad Date Row,-10.00,100.00
            2025-01-17,,Missing Description,-5.00,95.00
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var (result, transactions) = await _csvImporter.ParseCsvAsync(stream, "test.csv", "user123", "Checking");

        Assert.Equal(3, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(2, result.FailedCount);
        Assert.Single(transactions);
    }

    [Fact]
    public async Task Should_handle_empty_csv_file()
    {
        var csv = """
            Date,Description,Amount,Balance
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var (result, transactions) = await _csvImporter.ParseCsvAsync(stream, "test.csv", "user123", "Checking");

        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Empty(transactions);
    }

    [Fact]
    public async Task Should_parse_amounts_with_currency_symbols()
    {
        var csv = """
            Date,Description,Amount,Balance
            2025-01-15,Dollar Amount,$-45.67,$1250.33
            2025-01-16,Euro Amount,€100.00,€1350.33
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var (result, transactions) = await _csvImporter.ParseCsvAsync(stream, "test.csv", "user123", "Checking");

        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(-45.67m, transactions[0].Amount);
        Assert.Equal(100.00m, transactions[1].Amount);
    }
}
