using System.Text;
using System.Text.Json;

namespace BudgetTracker.Api.Tests.Extensions;

public static class JsonExtensions
{
    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    public static StringContent AsJsonContent(this object obj)
    {
        var json = JsonSerializer.Serialize(obj, JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
    
    public static async Task<T?> ToAsync<T>(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

}