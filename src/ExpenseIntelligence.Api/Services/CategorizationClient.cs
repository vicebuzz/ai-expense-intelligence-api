using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ExpenseIntelligence.Api.Services;

public record CategorizationSuggestion(string Category, string Source, double? Confidence);

public class CategorizationClient
{
    private readonly HttpClient _http;

    public CategorizationClient(HttpClient http) => _http = http;

    public static CategorizationSuggestion Fallback(bool isExpense) =>
        new("Other", "fallback", null);

    public async Task<CategorizationSuggestion> CategorizeAsync(
        string description,
        bool isExpense,
        IReadOnlyList<string>? allowedExpense = null,
        IReadOnlyList<string>? allowedIncome = null,
        CancellationToken cancellationToken = default)
    {
        var batch = await CategorizeBatchAsync(
            new[] { (description, isExpense) },
            allowedExpense,
            allowedIncome,
            cancellationToken);

        return batch.FirstOrDefault() ?? Fallback(isExpense);
    }

    public async Task<IReadOnlyList<CategorizationSuggestion>> CategorizeBatchAsync(
        IReadOnlyList<(string Description, bool IsExpense)> items,
        IReadOnlyList<string>? allowedExpense = null,
        IReadOnlyList<string>? allowedIncome = null,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
            return Array.Empty<CategorizationSuggestion>();

        try
        {
            var payload = new
            {
                items = items.Select(x => new
                {
                    description = x.Description,
                    is_expense = x.IsExpense
                }),
                allowed_expense_categories = allowedExpense,
                allowed_income_categories = allowedIncome
            };

            var response = await _http.PostAsJsonAsync("/categorize/batch", payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return items.Select(x => Fallback(x.IsExpense)).ToList();

            var body = await response.Content.ReadFromJsonAsync<BatchResponse>(cancellationToken: cancellationToken);
            if (body?.Results is null || body.Results.Count == 0)
                return items.Select(x => Fallback(x.IsExpense)).ToList();

            return body.Results
                .Select(r => new CategorizationSuggestion(
                    r.Category,
                    r.Source ?? "ml-service",
                    r.Confidence))
                .ToList();
        }
        catch
        {
            return items.Select(x => Fallback(x.IsExpense)).ToList();
        }
    }

    public async Task RetrainAsync(
        IReadOnlyList<(string Description, string Category)> samples,
        IReadOnlyList<string>? allowedCategories = null,
        CancellationToken cancellationToken = default)
    {
        if (samples.Count == 0)
            return;

        try
        {
            var payload = new
            {
                samples = samples.Select(s => new
                {
                    description = s.Description,
                    category = s.Category
                }),
                allowed_categories = allowedCategories
            };

            await _http.PostAsJsonAsync("/train", payload, cancellationToken);
        }
        catch
        {
            // ML service optional
        }
    }

    private sealed class BatchResponse
    {
        [JsonPropertyName("results")]
        public List<CategorizeResponse>? Results { get; set; }
    }

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
