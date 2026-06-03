namespace ExpenseIntelligence.Domain.Models;

public class CategoryCatalog
{
    public CategoryGroups Categories { get; set; } = new();
}

public class CategoryGroups
{
    public List<string> Outflow { get; set; } = new();
    public List<string> Inflow { get; set; } = new();
}
