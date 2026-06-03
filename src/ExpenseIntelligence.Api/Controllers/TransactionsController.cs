using ExpenseIntelligence.Api.Models;
using ExpenseIntelligence.Api.Services;
using ExpenseIntelligence.Domain.Entities;
using ExpenseIntelligence.Infrastructure.Persistence;
using ExpenseIntelligence.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ExpenseDbContext _db;
    private readonly CategoryCatalogService _categories;
    private readonly TransactionCategorizationService _categorization;

    public TransactionsController(
        ExpenseDbContext db,
        CategoryCatalogService categories,
        TransactionCategorizationService categorization)
    {
        _db = db;
        _categories = categories;
        _categorization = categorization;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransactionResponse>>> List(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? month,
        [FromQuery] string? category,
        [FromQuery] bool? isExpense,
        CancellationToken cancellationToken)
    {
        var query = _db.Transactions.AsNoTracking().AsQueryable();

        if (from.HasValue)
            query = query.Where(t => t.Date >= from.Value);
        if (to.HasValue)
            query = query.Where(t => t.Date <= to.Value);
        if (!string.IsNullOrWhiteSpace(month))
            query = query.Where(t => t.Month == month);
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(t => t.Category == category);
        if (isExpense.HasValue)
            query = query.Where(t => t.IsExpense == isExpense.Value);

        var items = await query
            .OrderByDescending(t => t.Date)
            .Take(500)
            .Select(t => ToResponse(t))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TransactionResponse>> Get(int id, CancellationToken cancellationToken)
    {
        var transaction = await _db.Transactions.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return transaction is null ? NotFound() : Ok(ToResponse(transaction));
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> Create(
        CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var category = request.Category;
        var source = "manual";

        if (string.IsNullOrWhiteSpace(category))
        {
            var suggestion = await _categorization.CategorizeAsync(
                request.Description,
                request.IsExpense,
                cancellationToken);
            category = suggestion.Category;
            source = suggestion.Source;
        }

        category = category.Trim();
        if (string.IsNullOrWhiteSpace(category))
            return BadRequest("Category is required (auto-categorisation could not assign a specific label).");

        if (!CategoryFilter.IsIncluded(category))
            return BadRequest($"Category '{CategoryFilter.Excluded}' is not allowed — choose a specific category.");

        var month = string.IsNullOrWhiteSpace(request.Month)
            ? Transaction.MonthFromDate(request.Date)
            : request.Month.Trim();

        var transaction = new Transaction
        {
            Date = request.Date,
            Month = month,
            Description = request.Description.Trim(),
            Amount = request.Amount,
            IsExpense = request.IsExpense,
            Category = category,
            CategorizationSource = source
        };

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = transaction.Id }, ToResponse(transaction));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var transaction = await _db.Transactions.FindAsync(new object[] { id }, cancellationToken);
        if (transaction is null)
            return NotFound();

        _db.Transactions.Remove(transaction);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static TransactionResponse ToResponse(Transaction t) =>
        new(
            t.Id,
            t.Date,
            string.IsNullOrEmpty(t.Month) ? Transaction.MonthFromDate(t.Date) : t.Month,
            t.Description,
            t.Amount,
            t.IsExpense ?? false,
            t.Category,
            t.CategorizationSource);
}
