using System.Globalization;
using System.Text;
using BudgetTracker.Api.Features.Transactions.Import.Detection;
using CsvHelper;
using CsvHelper.Configuration;

namespace BudgetTracker.Api.Features.Transactions.Import.Processing;

public class CsvImporter
{
    public Task<(ImportResult Result, List<Transaction> Transactions)> ParseCsvAsync(
        Stream csvStream, string sourceFileName, string userId, string account)
        => ParseCsvAsync(csvStream, sourceFileName, userId, account, null);

    public async Task<(ImportResult Result, List<Transaction> Transactions)> ParseCsvAsync(
        Stream csvStream, string sourceFileName, string userId, string account,
        CsvStructureDetectionResult? detectionResult)
    {
        var result = new ImportResult
        {
            SourceFile = sourceFileName,
            ImportedAt = DateTime.UtcNow
        };

        var transactions = new List<Transaction>();
        var delimiter = detectionResult?.Delimiter ?? ",";
        var culture = ParseCulture(detectionResult?.CultureCode);

        try
        {
            using var reader = new StreamReader(csvStream, Encoding.UTF8);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
                Delimiter = delimiter
            });

            var rowNumber = 0;

            await foreach (var record in csv.GetRecordsAsync<dynamic>())
            {
                rowNumber++;
                result.TotalRows++;

                try
                {
                    var transaction = ParseTransactionRow(record, detectionResult?.ColumnMappings, culture);
                    if (transaction != null)
                    {
                        transaction.UserId = userId;
                        transaction.Account = account;

                        transactions.Add(transaction);
                        result.ImportedCount++;
                    }
                    else
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Row {rowNumber}: Failed to parse transaction");
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Errors.Add($"Row {rowNumber}: {ex.Message}");
                }
            }

            result.ImportedCount = transactions.Count;
            result.FailedCount = result.TotalRows - result.ImportedCount;

            return (result, transactions);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"CSV parsing error: {ex.Message}");
            return (result, new List<Transaction>());
        }
    }

    private Transaction? ParseTransactionRow(
        dynamic record,
        Dictionary<string, string>? columnMappings,
        CultureInfo culture)
    {
        try
        {
            var recordDict = (IDictionary<string, object>)record;

            var description = ResolveColumn(recordDict, columnMappings, "Description",
                ColumnMappingDictionary.DescriptionColumns);
            var dateStr = ResolveColumn(recordDict, columnMappings, "Date",
                ColumnMappingDictionary.DateColumns);
            var amountStr = ResolveColumn(recordDict, columnMappings, "Amount",
                ColumnMappingDictionary.AmountColumns);
            var balanceStr = ResolveColumn(recordDict, columnMappings, "Balance",
                ColumnMappingDictionary.BalanceColumns);
            var category = ResolveColumn(recordDict, columnMappings, "Category",
                ColumnMappingDictionary.CategoryColumns);

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description is required");

            if (string.IsNullOrWhiteSpace(dateStr))
                throw new ArgumentException("Date is required");

            if (string.IsNullOrWhiteSpace(amountStr))
                throw new ArgumentException("Amount is required");

            if (!TryParseDate(dateStr, culture, out var date))
                throw new ArgumentException($"Invalid date format: {dateStr}");

            if (!TryParseAmount(amountStr, culture, out var amount))
                throw new ArgumentException($"Invalid amount format: {amountStr}");

            decimal? balance = null;
            if (!string.IsNullOrWhiteSpace(balanceStr) && TryParseAmount(balanceStr, culture, out var parsedBalance))
                balance = parsedBalance;

            return new Transaction
            {
                Id = Guid.NewGuid(),
                Date = date,
                Description = description.Trim(),
                Amount = amount,
                Balance = balance,
                Category = !string.IsNullOrWhiteSpace(category?.Trim()) ? category.Trim() : "Uncategorized",
                ImportedAt = DateTime.UtcNow,
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? ResolveColumn(
        IDictionary<string, object> record,
        Dictionary<string, string>? columnMappings,
        string logicalName,
        string[] defaultNames)
    {
        // Try the mapped column name first
        if (columnMappings != null && columnMappings.TryGetValue(logicalName, out var mappedName))
        {
            var value = GetColumnValue(record, mappedName);
            if (value != null) return value;
        }

        // Fall back to default column names
        return GetColumnValue(record, defaultNames);
    }

    private static string? GetColumnValue(IDictionary<string, object> record, params string[] columnNames)
    {
        foreach (var columnName in columnNames)
        {
            if (record.TryGetValue(columnName, out var value) && value != null)
                return value.ToString()?.Trim();
        }
        return null;
    }

    private static bool TryParseDate(string dateStr, CultureInfo culture, out DateTime date)
    {
        date = default;

        if (DateTime.TryParse(dateStr.Trim(), culture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date))
            return true;

        if (DateTime.TryParse(dateStr.Trim(), CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date))
            return true;

        return false;
    }

    private static bool TryParseAmount(string amountStr, CultureInfo culture, out decimal amount)
    {
        amount = 0;

        if (string.IsNullOrWhiteSpace(amountStr))
            return false;

        var cleanAmount = amountStr.Trim()
            .Replace("$", "").Replace("€", "").Replace("£", "").Replace("¥", "").Replace("R$", "").Trim();

        if (decimal.TryParse(cleanAmount, NumberStyles.Currency, culture, out amount))
            return true;

        if (decimal.TryParse(cleanAmount, NumberStyles.Currency, CultureInfo.InvariantCulture, out amount))
            return true;

        return false;
    }

    private static CultureInfo ParseCulture(string? cultureCode)
    {
        if (string.IsNullOrWhiteSpace(cultureCode))
            return CultureInfo.InvariantCulture;

        try
        {
            return CultureInfo.GetCultureInfo(cultureCode);
        }
        catch
        {
            return CultureInfo.InvariantCulture;
        }
    }
}
