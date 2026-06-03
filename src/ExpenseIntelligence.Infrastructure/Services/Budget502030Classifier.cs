namespace ExpenseIntelligence.Infrastructure.Services;

public static class Budget502030Classifier
{
    private static readonly HashSet<string> Necessities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Bills", "Education", "Food", "Groceries", "Health&Hygiene", "Health",
        "Rent", "Transport"
    };

    private static readonly HashSet<string> Wants = new(StringComparer.OrdinalIgnoreCase)
    {
        "Entertainment", "Presents", "Shopping", "Sports&Activities", "Sports",
        "Subscriptions", "Tobacco", "Travel&Tourism", "Travel"
    };

    public static string Classify(string category)
    {
        if (Necessities.Contains(category))
            return "Necessities";
        if (Wants.Contains(category))
            return "Wants";
        if (category.Equals("Savings", StringComparison.OrdinalIgnoreCase))
            return "Savings";
        return "Wants";
    }
}
