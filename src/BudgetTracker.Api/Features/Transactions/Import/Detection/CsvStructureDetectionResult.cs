namespace BudgetTracker.Api.Features.Transactions.Import.Detection;

public enum DetectionMethod
{
    RuleBased,
    AI
}

public class CsvStructureDetectionResult
{
    public DetectionMethod Method { get; set; }
    public double ConfidenceScore { get; set; }
    public string Delimiter { get; set; } = ",";
    public string CultureCode { get; set; } = "en-US";
    public Dictionary<string, string> ColumnMappings { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
