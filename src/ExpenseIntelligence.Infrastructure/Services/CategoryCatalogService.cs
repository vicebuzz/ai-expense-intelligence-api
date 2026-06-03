using System.Text.Json;
using ExpenseIntelligence.Domain.Entities;
using ExpenseIntelligence.Domain.Models;
using ExpenseIntelligence.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseIntelligence.Infrastructure.Services;

public class CategoryCatalogService
{
    private readonly ExpenseDbContext _db;

    public CategoryCatalogService(ExpenseDbContext db) => _db = db;

    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.CategoryDefinitions.AnyAsync(cancellationToken))
            return;

        var seed = LoadSeedFromFile();
        await ReplaceAllAsync(seed.Categories.Outflow, seed.Categories.Inflow, cancellationToken);
    }

    public async Task<(IReadOnlyList<string> Expense, IReadOnlyList<string> Income)> GetCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);

        var rows = await _db.CategoryDefinitions
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return (
            rows.Where(c => c.IsExpense).Select(c => c.Name).ToList(),
            rows.Where(c => !c.IsExpense).Select(c => c.Name).ToList());
    }

    public async Task<bool> IsValidCategoryAsync(
        string category,
        bool isExpense,
        CancellationToken cancellationToken = default)
    {
        var (expense, income) = await GetCatalogAsync(cancellationToken);
        var list = isExpense ? expense : income;
        return list.Contains(category, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<UpdateCatalogResult> UpdateCatalogAsync(
        IReadOnlyList<string> expense,
        IReadOnlyList<string> income,
        CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);

        var normalizedExpense = NormalizeList(expense);
        var normalizedIncome = NormalizeList(income);
        var warnings = new List<string>();

        if (normalizedExpense.Count == 0 && normalizedIncome.Count == 0)
            throw new InvalidOperationException("At least one category is required.");

        var (currentExpense, currentIncome) = await GetCatalogAsync(cancellationToken);

        foreach (var removed in currentExpense.Except(normalizedExpense, StringComparer.OrdinalIgnoreCase))
        {
            if (!CategoryFilter.IsIncluded(removed))
                continue;

            var inUse = await _db.Transactions.AnyAsync(
                t => t.IsExpense == true && t.Category == removed,
                cancellationToken);

            if (inUse)
                warnings.Add($"Expense category \"{removed}\" is still used on transactions and was removed from the catalog only.");
        }

        foreach (var removed in currentIncome.Except(normalizedIncome, StringComparer.OrdinalIgnoreCase))
        {
            if (!CategoryFilter.IsIncluded(removed))
                continue;

            var inUse = await _db.Transactions.AnyAsync(
                t => t.IsExpense == false && t.Category == removed,
                cancellationToken);

            if (inUse)
                warnings.Add($"Income category \"{removed}\" is still used on transactions and was removed from the catalog only.");
        }

        await ReplaceAllAsync(normalizedExpense, normalizedIncome, cancellationToken);

        return new UpdateCatalogResult(normalizedExpense, normalizedIncome, warnings);
    }

    private async Task ReplaceAllAsync(
        IReadOnlyList<string> expense,
        IReadOnlyList<string> income,
        CancellationToken cancellationToken)
    {
        await _db.CategoryDefinitions.ExecuteDeleteAsync(cancellationToken);

        var rows = new List<CategoryDefinition>();
        var order = 0;
        foreach (var name in expense)
        {
            rows.Add(new CategoryDefinition
            {
                Name = name,
                IsExpense = true,
                SortOrder = order++
            });
        }

        order = 0;
        foreach (var name in income)
        {
            rows.Add(new CategoryDefinition
            {
                Name = name,
                IsExpense = false,
                SortOrder = order++
            });
        }

        _db.CategoryDefinitions.AddRange(rows);
        await _db.SaveChangesAsync(cancellationToken);
        await PersistSeedFileAsync(expense, income, cancellationToken);
    }

    private static List<string> NormalizeList(IReadOnlyList<string> items)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var raw in items)
        {
            var name = raw?.Trim() ?? "";
            if (name.Length == 0)
                continue;

            if (name.Equals(CategoryFilter.Excluded, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Category \"{CategoryFilter.Excluded}\" cannot be added to the catalog.");

            if (!seen.Add(name))
                continue;

            result.Add(name);
        }

        return result;
    }

    private async Task PersistSeedFileAsync(
        IReadOnlyList<string> expense,
        IReadOnlyList<string> income,
        CancellationToken cancellationToken)
    {
        try
        {
            var path = ResolveSeedPath("categories.json");
            var payload = new
            {
                categories = new
                {
                    outflow = expense,
                    inflow = income
                }
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json, cancellationToken);
        }
        catch
        {
            // Seed file is a backup; DB is source of truth.
        }
    }

    private static CategoryCatalog LoadSeedFromFile()
    {
        var path = ResolveSeedPath("categories.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<CategoryCatalog>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new CategoryCatalog();
    }

    private static string ResolveSeedPath(string fileName)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "seed", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "seed", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "seed", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "seed", fileName),
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        throw new FileNotFoundException($"Seed file not found: {fileName}");
    }
}

public record UpdateCatalogResult(
    IReadOnlyList<string> Expense,
    IReadOnlyList<string> Income,
    IReadOnlyList<string> Warnings);
