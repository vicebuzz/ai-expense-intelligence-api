using ExpenseIntelligence.Infrastructure.Persistence;
using ExpenseIntelligence.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace ExpenseIntelligence.Api.Services;

public record CategoryHistoryMatch(string Category, int Count, int Total, double Share);

public class TransactionCategorizationService
{
    private const int MaxTrainingSamples = 5000;
    private const int MinHistoryCount = 2;

    private readonly ExpenseDbContext _db;
    private readonly CategoryCatalogService _catalog;
    private readonly CategorizationClient _ml;

    public TransactionCategorizationService(
        ExpenseDbContext db,
        CategoryCatalogService catalog,
        CategorizationClient ml)
    {
        _db = db;
        _catalog = catalog;
        _ml = ml;
    }

    public async Task<CategorizationSuggestion> CategorizeAsync(
        string description,
        bool isExpense,
        CancellationToken cancellationToken = default)
    {
        var batch = await CategorizeBatchAsync(
            new[] { (description, isExpense) },
            cancellationToken);

        return batch.FirstOrDefault() ?? CategorizationClient.Fallback(isExpense);
    }

    public async Task<IReadOnlyList<CategorizationSuggestion>> CategorizeBatchAsync(
        IReadOnlyList<(string Description, bool IsExpense)> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
            return Array.Empty<CategorizationSuggestion>();

        var (expenseAllowed, incomeAllowed) = await GetAllowedListsAsync(cancellationToken);
        var history = await BuildHistoryLookupAsync(cancellationToken);

        var results = new CategorizationSuggestion[items.Count];
        var needsMl = new List<(int Index, string Description, bool IsExpense)>();

        for (var i = 0; i < items.Count; i++)
        {
            var (description, isExpense) = items[i];
            var key = HistoryKey(description, isExpense);

            if (history.TryGetValue(key, out var match))
            {
                var allowed = isExpense ? expenseAllowed : incomeAllowed;
                if (IsAllowed(match.Category, allowed))
                {
                    results[i] = new CategorizationSuggestion(
                        match.Category,
                        "history",
                        Math.Round(match.Share, 4));
                    continue;
                }
            }

            needsMl.Add((i, description, isExpense));
        }

        if (needsMl.Count > 0)
        {
            var mlItems = needsMl.Select(x => (x.Description, x.IsExpense)).ToList();
            var mlResults = await _ml.CategorizeBatchAsync(
                mlItems,
                expenseAllowed,
                incomeAllowed,
                cancellationToken);

            for (var m = 0; m < needsMl.Count; m++)
            {
                var (index, description, isExpense) = needsMl[m];
                var suggestion = mlResults[m];
                var allowed = isExpense ? expenseAllowed : incomeAllowed;
                results[index] = ConstrainToCatalog(suggestion, allowed, isExpense);
            }
        }

        return results;
    }

    public async Task<int> RetrainFromDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var (expenseAllowed, incomeAllowed) = await GetAllowedListsAsync(cancellationToken);
        var allowedAll = expenseAllowed
            .Concat(incomeAllowed)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var rows = await _db.Transactions.AsNoTracking()
            .Where(t => t.Description != "" && t.Category != CategoryFilter.Excluded && t.IsExpense != null)
            .Select(t => new { t.Description, t.Category, t.IsExpense })
            .Take(MaxTrainingSamples * 2)
            .ToListAsync(cancellationToken);

        var samples = rows
            .Where(r => allowedAll.Contains(r.Category.Trim()))
            .Select(r => (r.Description.Trim(), r.Category.Trim()))

            .Distinct()
            .Take(MaxTrainingSamples)
            .ToList();

        await _ml.RetrainAsync(
            samples,
            expenseAllowed.Concat(incomeAllowed).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            cancellationToken);

        return samples.Count;
    }

    private async Task<(IReadOnlyList<string> Expense, IReadOnlyList<string> Income)> GetAllowedListsAsync(
        CancellationToken cancellationToken)
    {
        var (expense, income) = await _catalog.GetCatalogAsync(cancellationToken);
        return (
            expense.Where(CategoryFilter.IsIncluded).ToList(),
            income.Where(CategoryFilter.IsIncluded).ToList());
    }

    private async Task<Dictionary<(string Description, bool IsExpense), CategoryHistoryMatch>> BuildHistoryLookupAsync(
        CancellationToken cancellationToken)
    {
        var rows = await _db.Transactions.AsNoTracking()
            .Where(t => t.Description != "" && t.Category != CategoryFilter.Excluded && t.IsExpense != null)
            .Select(t => new { t.Description, t.IsExpense, t.Category })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => HistoryKey(r.Description, r.IsExpense!.Value))
            .Select(g =>
            {
                var top = g
                    .GroupBy(x => x.Category.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(c => new { Category = c.Key, Count = c.Count() })
                    .OrderByDescending(c => c.Count)
                    .First();

                return new
                {
                    g.Key,
                    top.Category,
                    top.Count,
                    Total = g.Count(),
                };
            })
            .Where(x => x.Count >= MinHistoryCount)
            .ToDictionary(
                x => x.Key,
                x => new CategoryHistoryMatch(
                    x.Category,
                    x.Count,
                    x.Total,
                    x.Count / (double)x.Total));
    }

    private static (string Description, bool IsExpense) HistoryKey(string description, bool isExpense) =>
        (description.Trim().ToUpperInvariant(), isExpense);

    private static bool IsAllowed(string category, IReadOnlyList<string> allowed) =>
        allowed.Contains(category, StringComparer.OrdinalIgnoreCase);

    private static CategorizationSuggestion ConstrainToCatalog(
        CategorizationSuggestion suggestion,
        IReadOnlyList<string> allowed,
        bool isExpense)
    {
        if (IsAllowed(suggestion.Category, allowed))
            return suggestion;

        if (allowed.Count > 0)
        {
            return new CategorizationSuggestion(
                allowed[0],
                $"{suggestion.Source}-catalog-default",
                suggestion.Confidence);
        }

        return CategorizationClient.Fallback(isExpense);
    }
}
