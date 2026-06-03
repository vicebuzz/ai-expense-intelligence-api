using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ExpenseIntelligence.Api.Services;

public record CategorizationSuggestion(string Category, string Source, double? Confidence);

public class CategorizationClient
{
    private readonly HttpClient _http;

    public CategorizationClient(HttpClient http) => _http = http;

    public async Task<CategorizationSuggestion> CategorizeAsync(
        string description,
        bool isExpense,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(
                "/categorize",
                new { description, is_expense = isExpense },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Fallback(isExpense);

            var body = await response.Content.ReadFromJsonAsync<CategorizeResponse>(cancellationToken: cancellationToken);
            if (body is null || string.IsNullOrWhiteSpace(body.Category))
                return Fallback(isExpense);

            return new CategorizationSuggestion(body.Category, body.Source ?? "ml-service", body.Confidence);
        }
        catch
        {
            return Fallback(isExpense);
        }
    }

    private static CategorizationSuggestion Fallback(bool isExpense) =>
        new(isExpense ? "Other" : "Other", "fallback", null);

    private sealed class CategorizeResponse
    {
        [JsonPropertyName("category")]
        public string Category { get; set; } = "";

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("confidence")]
        public double? Confidence { get; set; }
    }
}
