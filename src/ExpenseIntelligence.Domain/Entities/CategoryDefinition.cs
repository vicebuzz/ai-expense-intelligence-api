namespace ExpenseIntelligence.Domain.Entities;

public class CategoryDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsExpense { get; set; }
    public int SortOrder { get; set; }
}
