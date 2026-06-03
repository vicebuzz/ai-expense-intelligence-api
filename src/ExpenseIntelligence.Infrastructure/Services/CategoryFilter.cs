namespace ExpenseIntelligence.Infrastructure.Services;

/// <summary>
/// Catch-all category excluded from analytics and ML — too generic for useful insights.
/// </summary>
public static class CategoryFilter
{
    public const string Excluded = "Other";

    public static bool IsIncluded(string? category) =>
        !string.IsNullOrWhiteSpace(category) &&
        !category.Trim().Equals(Excluded, StringComparison.OrdinalIgnoreCase);
}
