namespace ExpenseIntelligence.Api.Models;

public record TransactionResponse(
    int Id,
    DateOnly Date,
    string Description,
    decimal Amount,
    bool IsExpense,
    string Category,
    string? CategorizationSource);

public record CreateTransactionRequest(
    DateOnly Date,
    string Description,
    decimal Amount,
    bool IsExpense,
    string? Category);
