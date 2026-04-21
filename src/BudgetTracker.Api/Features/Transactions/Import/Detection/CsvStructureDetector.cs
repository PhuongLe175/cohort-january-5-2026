using System.Text;

namespace BudgetTracker.Api.Features.Transactions.Import.Detection;

public class CsvStructureDetector(ICsvDetector aiDetector, ILogger<CsvStructureDetector> logger) : ICsvStructureDetector
{
    private const double RuleBasedConfidenceThreshold = 85;

    public async Task<CsvStructureDetectionResult> DetectStructureAsync(Stream csvStream)
    {
        try
        {
            logger.LogDebug("Starting CSV structure detection");

            var simpleResult = TrySimpleParsing(csvStream);

            if (simpleResult.ConfidenceScore >= RuleBasedConfidenceThreshold)
            {
                logger.LogDebug("Simple parsing successful with {Confidence}% confidence", simpleResult.ConfidenceScore);
                return simpleResult;
            }

            logger.LogDebug("Simple parsing failed (confidence: {Confidence}%), falling back to AI detection",
                simpleResult.ConfidenceScore);

            csvStream.Position = 0;
            return await aiDetector.AnalyzeCsvStructureAsync(csvStream);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during CSV structure detection, attempting AI fallback");
            csvStream.Position = 0;
            return await aiDetector.AnalyzeCsvStructureAsync(csvStream);
        }
    }

    private static CsvStructureDetectionResult TrySimpleParsing(Stream csvStream)
    {
        csvStream.Position = 0;
        using var reader = new StreamReader(csvStream, Encoding.UTF8, leaveOpen: true);

        var lines = new List<string>();
        for (var i = 0; i < 100 && !reader.EndOfStream; i++)
        {
            var line = reader.ReadLine();
            if (!string.IsNullOrWhiteSpace(line))
                lines.Add(line);
        }

        if (lines.Count < 1)
            return new CsvStructureDetectionResult { ConfidenceScore = 0, Method = DetectionMethod.RuleBased };

        var result = new CsvStructureDetectionResult
        {
            Method = DetectionMethod.RuleBased,
            Delimiter = ",",
            CultureCode = "en-US"
        };

        // Rule-based uses comma only — non-English/non-comma files fall through to AI
        var headers = lines[0].Split(',').Select(h => h.Trim().Trim('"')).ToArray();

        var dateColumn = FindColumn(headers, ColumnMappingDictionary.DateColumns);
        var descriptionColumn = FindColumn(headers, ColumnMappingDictionary.DescriptionColumns);
        var amountColumn = FindColumn(headers, ColumnMappingDictionary.AmountColumns);

        if (dateColumn == null || descriptionColumn == null || amountColumn == null)
        {
            result.ConfidenceScore = 0;
            return result;
        }

        result.ColumnMappings["Date"] = dateColumn;
        result.ColumnMappings["Description"] = descriptionColumn;
        result.ColumnMappings["Amount"] = amountColumn;

        var balanceColumn = FindColumn(headers, ColumnMappingDictionary.BalanceColumns);
        if (balanceColumn != null) result.ColumnMappings["Balance"] = balanceColumn;

        var categoryColumn = FindColumn(headers, ColumnMappingDictionary.CategoryColumns);
        if (categoryColumn != null) result.ColumnMappings["Category"] = categoryColumn;

        // Validate by trying to parse sample data rows
        var sampleRows = lines.Skip(1).Take(3).ToList();
        var successfulParses = 0;

        foreach (var row in sampleRows)
        {
            var parts = row.Split(',');
            if (parts.Length >= headers.Length && TryParseRow(parts, headers, result.ColumnMappings))
                successfulParses++;
        }

        result.ConfidenceScore = sampleRows.Count > 0
            ? (double)successfulParses / sampleRows.Count * 100
            : 85; // Headers matched but no data rows — give passing score

        return result;
    }

    private static string? FindColumn(string[] headers, string[] patterns)
    {
        return headers.FirstOrDefault(header =>
            patterns.Any(pattern =>
                string.Equals(pattern, header.Trim(), StringComparison.OrdinalIgnoreCase)));
    }

    private static bool TryParseRow(string[] parts, string[] headers, Dictionary<string, string> mappings)
    {
        try
        {
            if (mappings.TryGetValue("Date", out var dateColumn))
            {
                var dateIndex = Array.IndexOf(headers, dateColumn);
                if (dateIndex >= 0 && dateIndex < parts.Length)
                {
                    var dateStr = parts[dateIndex].Trim().Trim('"');
                    if (!DateTime.TryParse(dateStr, System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out _))
                        return false;
                }
            }

            if (mappings.TryGetValue("Amount", out var amountColumn))
            {
                var amountIndex = Array.IndexOf(headers, amountColumn);
                if (amountIndex >= 0 && amountIndex < parts.Length)
                {
                    var amountStr = parts[amountIndex].Trim().Trim('"').Replace("$", "").Replace(",", "");
                    if (!decimal.TryParse(amountStr, System.Globalization.NumberStyles.Number,
                            System.Globalization.CultureInfo.InvariantCulture, out _))
                        return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
