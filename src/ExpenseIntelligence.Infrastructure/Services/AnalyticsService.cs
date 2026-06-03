using ExpenseIntelligence.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseIntelligence.Infrastructure.Services;

public record MonthlyCategorySpend(string Month, string Category, decimal Total);

public record AverageMonthlyCategorySpend(string Category, decimal AverageAmount);

public record TopMerchantInsight(
    string Description,
    decimal TotalAmount,
    int TransactionCount,
    IReadOnlyList<string> Categories);

public class AnalyticsService
{
    private readonly ExpenseDbContext _db;

    public AnalyticsService(ExpenseDbContext db) => _db = db;

    public async Task<IReadOnlyList<MonthlyCategorySpend>> GetSpendingByCategoryAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Transactions.AsNoTracking().Where(t => t.IsExpense);

        if (from.HasValue)
            query = query.Where(t => t.Date >= from.Value);
        if (to.HasValue)
            query = query.Where(t => t.Date <= to.Value);

        return await query
            .GroupBy(t => new { Month = t.Date.ToString("yyyy-MM"), t.Category })
            .Select(g => new MonthlyCategorySpend(g.Key.Month, g.Key.Category, g.Sum(t => t.Amount)))
            .OrderBy(x => x.Month)
            .ThenBy(x => x.Category)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AverageMonthlyCategorySpend>> GetAverageMonthlySpendByCategoryAsync(
        CancellationToken cancellationToken = default)
    {
        var monthlyTotals = await _db.Transactions.AsNoTracking()
            .Where(t => t.IsExpense)
            .GroupBy(t => new { Month = t.Date.ToString("yyyy-MM"), t.Category })
            .Select(g => new { g.Key.Category, Total = g.Sum(t => t.Amount) })
            .ToListAsync(cancellationToken);

        return monthlyTotals
            .GroupBy(x => x.Category)
            .Select(g => new AverageMonthlyCategorySpend(
                g.Key,
                Math.Round(g.Average(x => x.Total), 2)))
            .OrderBy(x => x.Category)
            .ToList();
    }

    public async Task<IReadOnlyList<TopMerchantInsight>> GetTopMerchantsAsync(
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        return await _db.Transactions.AsNoTracking()
            .Where(t => t.IsExpense)
            .GroupBy(t => t.Description)
            .Select(g => new TopMerchantInsight(
                g.Key,
                g.Sum(t => t.Amount),
                g.Count(),
                g.Select(t => t.Category).Distinct().ToList()))
            .OrderByDescending(x => x.TotalAmount)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
