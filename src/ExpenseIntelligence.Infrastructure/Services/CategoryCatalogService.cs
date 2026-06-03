using System.Text.Json;
using ExpenseIntelligence.Domain.Models;
namespace ExpenseIntelligence.Infrastructure.Services;

public class CategoryCatalogService
{
    private readonly CategoryCatalog _catalog;

    public CategoryCatalogService()
    {
        var path = ResolveSeedPath("categories.json");

        var json = File.ReadAllText(path);
        _catalog = JsonSerializer.Deserialize<CategoryCatalog>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new CategoryCatalog();
    }

    public IReadOnlyList<string> GetExpenseCategories() => _catalog.Categories.Outflow;

    public IReadOnlyList<string> GetIncomeCategories() => _catalog.Categories.Inflow;

    public bool IsValidCategory(string category, bool isExpense) =>
        (isExpense ? GetExpenseCategories() : GetIncomeCategories())
            .Contains(category, StringComparer.OrdinalIgnoreCase);

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
