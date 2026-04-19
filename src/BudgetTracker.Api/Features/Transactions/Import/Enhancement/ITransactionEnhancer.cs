namespace BudgetTracker.Api.Features.Transactions.Import.Enhancement;

public interface ITransactionEnhancer
{
    Task<IReadOnlyList<EnhancedTransactionDescription>> EnhanceDescriptionsAsync(
        IReadOnlyList<string> rawDescriptions,
        CancellationToken cancellationToken = default);
}

public record EnhancedTransactionDescription(
    string OriginalDescription,
    string CleanDescription,
    string SuggestedCategory);
