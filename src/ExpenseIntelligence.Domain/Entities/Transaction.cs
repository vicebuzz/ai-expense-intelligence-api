namespace ExpenseIntelligence.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsExpense { get; set; }
    public string Category { get; set; } = string.Empty;

    /// <summary>Only stored when using the portfolio schema (not legacy DB).</summary>
    public string? CategorizationSource { get; set; }

    /// <summary>Only stored when using the portfolio schema (not legacy DB).</summary>
    public DateTime? CreatedAt { get; set; }
}
