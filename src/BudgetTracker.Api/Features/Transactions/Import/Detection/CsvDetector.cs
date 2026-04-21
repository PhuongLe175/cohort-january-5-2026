using System.Text;
using System.Text.Json;
using BudgetTracker.Api.Features.Transactions.Import.Processing;
using BudgetTracker.Api.Infrastructure.Extensions;

namespace BudgetTracker.Api.Features.Transactions.Import.Detection;

public class CsvDetector(ICsvAnalyzer analyzer, ILogger<CsvDetector> logger) : ICsvDetector
{
    public async Task<CsvStructureDetectionResult> AnalyzeCsvStructureAsync(Stream csvStream)
    {
        try
        {
            logger.LogDebug("Starting AI CSV structure analysis");

            csvStream.Position = 0;
            using var reader = new StreamReader(csvStream, Encoding.UTF8, leaveOpen: true);

            var lines = new List<string>();
            for (var i = 0; i < 5; i++)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;
                if (!string.IsNullOrWhiteSpace(line))
                    lines.Add(line);
            }

            if (lines.Count == 0)
            {
                logger.LogWarning("No data found in CSV for AI analysis");
                return new CsvStructureDetectionResult { ConfidenceScore = 0, Method = DetectionMethod.AI };
            }

            logger.LogDebug("Sending CSV structure analysis request to AI service");

            var csvContent = string.Join("\n", lines);
            var responseText = await analyzer.AnalyzeCsvStructureAsync(csvContent);

            if (string.IsNullOrEmpty(responseText))
            {
                logger.LogWarning("AI service returned empty response for CSV structure analysis");
                return new CsvStructureDetectionResult { ConfidenceScore = 0, Method = DetectionMethod.AI };
            }

            var result = ParseAiResponse(responseText.ExtractJsonFromCodeBlock());
            result.Method = DetectionMethod.AI;

            logger.LogDebug("AI detection completed - confidence: {Confidence}%, delimiter: '{Delimiter}', culture: '{Culture}'",
                result.ConfidenceScore, result.Delimiter, result.CultureCode);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI CSV structure analysis failed");
            return new CsvStructureDetectionResult { ConfidenceScore = 0, Method = DetectionMethod.AI };
        }
    }

    private CsvStructureDetectionResult ParseAiResponse(string aiResponse)
    {
        try
        {
            var jsonStart = aiResponse.IndexOf('{');
            var jsonEnd = aiResponse.LastIndexOf('}');

            if (jsonStart < 0 || jsonEnd <= jsonStart)
            {
                logger.LogWarning("Could not find JSON object in AI response");
                return new CsvStructureDetectionResult { ConfidenceScore = 0, Method = DetectionMethod.AI };
            }

            var jsonContent = aiResponse[jsonStart..(jsonEnd + 1)];
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            var result = new CsvStructureDetectionResult { Method = DetectionMethod.AI };

            // CsvAnalyzer returns "delimiter" (not "columnSeparator")
            if (root.TryGetProperty("delimiter", out var delimProp))
            {
                result.Delimiter = delimProp.GetString() ?? ",";
                if (result.Delimiter == "\\t") result.Delimiter = "\t";
            }

            if (root.TryGetProperty("cultureCode", out var cultureCode))
                result.CultureCode = cultureCode.GetString() ?? "en-US";

            // CsvAnalyzer returns columnMappings as { "CsvColName": "StandardField" }
            // CsvStructureDetectionResult.ColumnMappings needs the inverse: { "StandardField": "CsvColName" }
            if (root.TryGetProperty("columnMappings", out var mappingsProp))
            {
                foreach (var mapping in mappingsProp.EnumerateObject())
                {
                    var csvColumnName = mapping.Name;           // e.g. "Data"
                    var standardField = mapping.Value.GetString(); // e.g. "Date"
                    if (!string.IsNullOrEmpty(standardField))
                        result.ColumnMappings[standardField] = csvColumnName;
                }
            }

            // CsvAnalyzer returns confidenceScore as 0-1; threshold checks use 0-100 scale
            if (root.TryGetProperty("confidenceScore", out var confidence))
            {
                var raw = confidence.GetDouble();
                result.ConfidenceScore = raw <= 1.0 ? raw * 100 : raw;
            }

            logger.LogDebug("AI detection parsed - delimiter: '{Delimiter}', culture: '{Culture}', confidence: {Confidence}%",
                result.Delimiter, result.CultureCode, result.ConfidenceScore);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse AI response JSON: {Response}", aiResponse);
            return new CsvStructureDetectionResult { ConfidenceScore = 0, Method = DetectionMethod.AI };
        }
    }
}
