using System.Text.Json;
using Microsoft.Extensions.AI;

namespace BudgetTracker.Api.Features.Transactions.Import.Enhancement;

public class TransactionEnhancer(IChatClient chatClient) : ITransactionEnhancer
{
    private const string SystemPrompt = """
        You are a financial transaction categorizer. Given raw bank transaction descriptions,
        return a JSON array where each element has:
        - "original": the original description (unchanged)
        - "clean": a human-readable description (e.g. "Amazon Purchase" instead of "AMZN MKTP US*123456789")
        - "category": a spending category (e.g. Shopping, Groceries, Dining, Transport, Entertainment, Utilities, Healthcare, Transfer, Other)

        Respond with ONLY the JSON array, no additional text.
        """;

    public async Task<IReadOnlyList<EnhancedTransactionDescription>> EnhanceDescriptionsAsync(
        IReadOnlyList<string> rawDescriptions,
        CancellationToken cancellationToken = default)
    {
        if (rawDescriptions.Count == 0)
            return [];

        try
        {
            var userPrompt = $"Enhance these {rawDescriptions.Count} transaction descriptions:\n" +
                             string.Join("\n", rawDescriptions.Select((d, i) => $"{i + 1}. {d}"));

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, SystemPrompt),
                new(ChatRole.User, userPrompt)
            };

            var response = await chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
            var content = response.Text?.Trim() ?? string.Empty;

            return ParseResponse(content, rawDescriptions);
        }
        catch
        {
            return rawDescriptions
                .Select(d => new EnhancedTransactionDescription(d, d, "Other"))
                .ToList();
        }
    }

    private static IReadOnlyList<EnhancedTransactionDescription> ParseResponse(
        string content,
        IReadOnlyList<string> originals)
    {
        try
        {
            if (content.StartsWith("```"))
            {
                var start = content.IndexOf('[');
                var end = content.LastIndexOf(']');
                if (start >= 0 && end > start)
                    content = content[start..(end + 1)];
            }

            var items = JsonSerializer.Deserialize<List<JsonElement>>(content);
            if (items == null || items.Count != originals.Count)
                return Fallback(originals);

            return items.Select((item, i) => new EnhancedTransactionDescription(
                originals[i],
                item.TryGetProperty("clean", out var clean) ? clean.GetString() ?? originals[i] : originals[i],
                item.TryGetProperty("category", out var cat) ? cat.GetString() ?? "Other" : "Other"
            )).ToList();
        }
        catch
        {
            return Fallback(originals);
        }
    }

    private static IReadOnlyList<EnhancedTransactionDescription> Fallback(IReadOnlyList<string> originals) =>
        originals.Select(d => new EnhancedTransactionDescription(d, d, "Other")).ToList();
}
