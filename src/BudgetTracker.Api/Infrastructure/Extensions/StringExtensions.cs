using System.Text.RegularExpressions;

namespace BudgetTracker.Api.Infrastructure.Extensions;

public static class StringExtensions
{
    public static string ExtractJsonFromCodeBlock(this string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        var match = Regex.Match(text, @"```(?:json)?\s*([\s\S]*?)\s*```");
        if (match.Success)
            return match.Groups[1].Value.Trim();

        var objectMatch = Regex.Match(text, @"\{[\s\S]*\}");
        if (objectMatch.Success)
            return objectMatch.Value;

        return text.Trim();
    }
}
