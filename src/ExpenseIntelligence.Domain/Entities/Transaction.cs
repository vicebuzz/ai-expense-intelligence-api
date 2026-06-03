namespace ExpenseIntelligence.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }

    /// <summary>YYYY-MM — matches legacy accountbalancemanagement.month column.</summary>
    public string Month { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    /// <summary>Legacy DB allows NULL on expenditure; treat null as unknown in queries.</summary>
    public bool? IsExpense { get; set; }

    public bool IsExpenseOrDefault(bool defaultValue = true) => IsExpense ?? defaultValue;
    public string Category { get; set; } = string.Empty;

    public string? CategorizationSource { get; set; }
    public DateTime? CreatedAt { get; set; }

    public static string MonthFromDate(DateOnly date) =>
        $"{date.Year}-{date.Month:D2}";
}
