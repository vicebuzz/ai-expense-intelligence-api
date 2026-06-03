namespace ExpenseIntelligence.Api.Models;

public record UpdateCategoriesRequest(
    IReadOnlyList<string> Expense,
    IReadOnlyList<string> Income);

public record CategoryCatalogResponse(
    IReadOnlyList<string> Expense,
    IReadOnlyList<string> Income);

public record UpdateCategoriesResult(
    IReadOnlyList<string> Expense,
    IReadOnlyList<string> Income,
    IReadOnlyList<string> Warnings);
