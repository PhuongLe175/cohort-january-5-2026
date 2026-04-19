namespace BudgetTracker.Api.Features.Transactions.Import;

public class ImportResult
{
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? SourceFile { get; set; }
    public string ImportSessionHash { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; }
    public List<TransactionEnhancementResult> Enhancements { get; set; } = new();
}

public class TransactionEnhancementResult
{
    public Guid TransactionId { get; set; }
    public string OriginalDescription { get; set; } = string.Empty;
    public string CleanDescription { get; set; } = string.Empty;
    public string SuggestedCategory { get; set; } = string.Empty;
}

public class EnhanceImportRequest
{
    public string ImportSessionHash { get; set; } = string.Empty;
}

public class EnhanceImportResult
{
    public int UpdatedCount { get; set; }
    public string ImportSessionHash { get; set; } = string.Empty;
}
