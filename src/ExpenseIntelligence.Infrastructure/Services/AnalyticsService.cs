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

public record CategoryTotal(string Category, decimal Total);

public record MonthLabel(string Value, string Label);

public record DailySpend(DateOnly Date, decimal Total);

public record MonthlyEarningsVsExpenditure(string Month, decimal Earnings, decimal Expenditures);

public record BudgetGroupTotal(string Group, decimal Total);

public record CategoryPivotRow(string Month, Dictionary<string, decimal> Values);

public record CategoryPivotResult(
    IReadOnlyList<string> Months,
    IReadOnlyList<string> Categories,
    IReadOnlyList<CategoryPivotRow> Rows);

public record CategoryMonthRow(string Month, string Category, decimal TotalAmount, int Count);

public record ExpensesVsEarnings(decimal TotalExpenses, decimal TotalEarnings);

public class AnalyticsService
{
    private readonly ExpenseDbContext _db;

    public AnalyticsService(ExpenseDbContext db) => _db = db;

    private IQueryable<Domain.Entities.Transaction> Expenses =>
        _db.Transactions.AsNoTracking()
            .Where(t => t.IsExpense == true && t.Category != CategoryFilter.Excluded);

    private IQueryable<Domain.Entities.Transaction> Income =>
        _db.Transactions.AsNoTracking()
            .Where(t => t.IsExpense == false && t.Category != CategoryFilter.Excluded);

    public async Task<IReadOnlyList<MonthLabel>> GetAvailableMonthsAsync(CancellationToken cancellationToken = default)
    {
        var yearMonths = await _db.Transactions.AsNoTracking()
            .Select(t => new { t.Date.Year, t.Date.Month })
            .Distinct()
            .ToListAsync(cancellationToken);

        return yearMonths
            .Select(x => new DateOnly(x.Year, x.Month, 1))
            .Distinct()
            .OrderByDescending(d => d)
            .Select(d => new MonthLabel(
                FormatMonthKey(d.Year, d.Month),
                d.ToString("MMMM yyyy")))
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetExpenseCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await Expenses
            .Select(t => t.Category)
            .Distinct()
            .ToListAsync(cancellationToken);

        return categories
            .Select(c => c.Trim())
            .Where(CategoryFilter.IsIncluded)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
    }

    public async Task<IReadOnlyList<CategoryTotal>> GetSpendByCategoryAsync(
        string? month = null,
        CancellationToken cancellationToken = default)
    {
        var query = Expenses;
        query = ApplyMonthFilter(query, month);

        var rows = await query
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
            .OrderByDescending(x => x.Total)
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new CategoryTotal(x.Category.Trim(), x.Total))
            .ToList();
    }

    public async Task<IReadOnlyList<BudgetGroupTotal>> Get502030ActualAsync(
        string? month = null,
        CancellationToken cancellationToken = default)
    {
        var byCategory = await GetSpendByCategoryAsync(month, cancellationToken);

        return byCategory
            .GroupBy(x => Budget502030Classifier.Classify(x.Category))
            .Select(g => new BudgetGroupTotal(g.Key, g.Sum(x => x.Total)))
            .OrderBy(x => x.Group)
            .ToList();
    }

    public async Task<IReadOnlyList<BudgetGroupTotal>> Get502030TargetAsync(
        string? month = null,
        CancellationToken cancellationToken = default)
    {
        var query = Income;
        query = ApplyMonthFilter(query, month);

        var income = await query.SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        return new List<BudgetGroupTotal>
        {
            new("Necessities", Math.Round(income * 0.50m, 2)),
            new("Wants", Math.Round(income * 0.30m, 2)),
            new("Savings", Math.Round(income * 0.20m, 2))
        };
    }

    public async Task<decimal?> GetLatestSavingsAsync(CancellationToken cancellationToken = default)
    {
        if (!_db.UsesLegacySchema)
            return null;

        try
        {
            await using var connection = _db.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT savings_amount FROM savings WHERE date = (SELECT MAX(date) FROM savings) LIMIT 1";

            var scalar = await command.ExecuteScalarAsync(cancellationToken);
            return scalar is null or DBNull ? null : Convert.ToDecimal(scalar);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<DailySpend>> GetDailyExpendituresAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = Expenses;
        if (from.HasValue)
            query = query.Where(t => t.Date >= from.Value);
        if (to.HasValue)
            query = query.Where(t => t.Date <= to.Value);

        var rows = await query
            .GroupBy(t => t.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(t => t.Amount) })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new DailySpend(x.Date, x.Total)).ToList();
    }

    public async Task<IReadOnlyList<MonthlyCategorySpend>> GetMonthlySalaryAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await Income
            .Where(t => t.Category == "Wages" || t.Category == "Salary")
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Total = g.Sum(t => t.Amount)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new MonthlyCategorySpend(
                FormatMonthKey(x.Year, x.Month),
                "Salary",
                x.Total))
            .ToList();
    }

    public async Task<IReadOnlyList<MonthlyEarningsVsExpenditure>> GetEarningsVsExpendituresByMonthAsync(
        CancellationToken cancellationToken = default)
    {
        var expenseRows = await Expenses
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(t => t.Amount) })
            .ToListAsync(cancellationToken);

        var incomeRows = await Income
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(t => t.Amount) })
            .ToListAsync(cancellationToken);

        var months = expenseRows
            .Select(x => (x.Year, x.Month))
            .Concat(incomeRows.Select(x => (x.Year, x.Month)))
            .Distinct()
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month);

        return months.Select(m =>
        {
            var monthKey = FormatMonthKey(m.Year, m.Month);
            var earnings = incomeRows
                .Where(x => x.Year == m.Year && x.Month == m.Month)
                .Select(x => x.Total)
                .FirstOrDefault();
            var expenditures = expenseRows
                .Where(x => x.Year == m.Year && x.Month == m.Month)
                .Select(x => x.Total)
                .FirstOrDefault();
            return new MonthlyEarningsVsExpenditure(monthKey, earnings, expenditures);
        }).ToList();
    }

    public async Task<CategoryPivotResult> GetCategoryPivotAsync(
        string metric = "amount",
        CancellationToken cancellationToken = default)
    {
        var useCount = metric.Equals("count", StringComparison.OrdinalIgnoreCase);

        var raw = await Expenses
            .GroupBy(t => new { t.Date.Year, t.Date.Month, t.Category })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Category,
                Value = useCount ? g.Count() : g.Sum(t => t.Amount)
            })
            .ToListAsync(cancellationToken);

        var projected = raw.Select(x => new
        {
            Month = FormatMonthKey(x.Year, x.Month),
            Category = x.Category.Trim(),
            Value = useCount ? (decimal)x.Value : x.Value
        }).ToList();

        var months = projected.Select(x => x.Month).Distinct().OrderBy(m => m).ToList();
        var categories = projected.Select(x => x.Category).Distinct().OrderBy(c => c).ToList();

        var rows = months.Select(month =>
        {
            var values = categories.ToDictionary(
                c => c,
                c => projected
                    .Where(x => x.Month == month && x.Category == c)
                    .Select(x => x.Value)
                    .FirstOrDefault());
            return new CategoryPivotRow(month, values);
        }).ToList();

        return new CategoryPivotResult(months, categories, rows);
    }

    public async Task<IReadOnlyList<CategoryMonthRow>> GetCategoryMonthTableAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await Expenses
            .GroupBy(t => new { t.Date.Year, t.Date.Month, t.Category })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Category,
                Total = g.Sum(t => t.Amount),
                Count = g.Count()
            })
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new CategoryMonthRow(
                FormatMonthKey(x.Year, x.Month),
                x.Category.Trim(),
                x.Total,
                x.Count))
            .ToList();
    }

    public async Task<ExpensesVsEarnings> GetExpensesVsEarningsTotalsAsync(
        CancellationToken cancellationToken = default)
    {
        var totalExpenses = await Expenses.SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;
        var totalEarnings = await Income.SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        return new ExpensesVsEarnings(totalExpenses, totalEarnings);
    }

    public async Task<IReadOnlyList<MonthlyCategorySpend>> GetMonthlySpendForCategoriesAsync(
        IReadOnlyList<string> categories,
        CancellationToken cancellationToken = default)
    {
        if (categories.Count == 0)
            return Array.Empty<MonthlyCategorySpend>();

        var normalized = categories.Select(c => c.Trim()).ToList();

        var rows = await Expenses
            .Where(t => normalized.Contains(t.Category))
            .GroupBy(t => new { t.Date.Year, t.Date.Month, t.Category })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Category,
                Total = g.Sum(t => t.Amount)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ThenBy(x => x.Category)
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new MonthlyCategorySpend(
                FormatMonthKey(x.Year, x.Month),
                x.Category.Trim(),
                x.Total))
            .ToList();
    }

    public async Task<IReadOnlyList<MonthlyCategorySpend>> GetSpendingByCategoryAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = Expenses;

        if (from.HasValue)
            query = query.Where(t => t.Date >= from.Value);
        if (to.HasValue)
            query = query.Where(t => t.Date <= to.Value);

        var rows = await query
            .GroupBy(t => new { t.Date.Year, t.Date.Month, t.Category })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Category,
                Total = g.Sum(t => t.Amount)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ThenBy(x => x.Category)
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new MonthlyCategorySpend(
                FormatMonthKey(x.Year, x.Month),
                x.Category.Trim(),
                x.Total))
            .ToList();
    }

    public async Task<IReadOnlyList<AverageMonthlyCategorySpend>> GetAverageMonthlySpendByCategoryAsync(
        CancellationToken cancellationToken = default)
    {
        var monthlyTotals = await Expenses
            .GroupBy(t => new { t.Date.Year, t.Date.Month, t.Category })
            .Select(g => new { g.Key.Category, Total = g.Sum(t => t.Amount) })
            .ToListAsync(cancellationToken);

        return monthlyTotals
            .GroupBy(x => x.Category.Trim())
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
        var top = await Expenses
            .GroupBy(t => t.Description)
            .Select(g => new
            {
                Description = g.Key,
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count()
            })
            .OrderByDescending(x => x.TotalAmount)
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (top.Count == 0)
            return Array.Empty<TopMerchantInsight>();

        var descriptions = top.Select(t => t.Description).ToList();
        var categoryRows = await Expenses
            .Where(t => descriptions.Contains(t.Description))
            .Select(t => new { t.Description, t.Category })
            .ToListAsync(cancellationToken);

        var categoriesByDescription = categoryRows
            .GroupBy(x => x.Description)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.Select(x => x.Category.Trim()).Distinct().ToList());

        return top
            .Select(t => new TopMerchantInsight(
                t.Description,
                t.TotalAmount,
                t.TransactionCount,
                categoriesByDescription.GetValueOrDefault(t.Description, Array.Empty<string>())))
            .ToList();
    }

    private static string FormatMonthKey(int year, int month) =>
        $"{year}-{month:D2}";

    private static IQueryable<Domain.Entities.Transaction> ApplyMonthFilter(
        IQueryable<Domain.Entities.Transaction> query,
        string? month)
    {
        if (string.IsNullOrWhiteSpace(month) || !TryParseMonth(month, out var year, out var monthNum))
            return query;

        return query.Where(t => t.Date.Year == year && t.Date.Month == monthNum);
    }

    private static bool TryParseMonth(string month, out int year, out int monthNum)
    {
        year = 0;
        monthNum = 0;
        if (DateOnly.TryParseExact($"{month}-01", "yyyy-MM-dd", out var date))
        {
            year = date.Year;
            monthNum = date.Month;
            return true;
        }

        return false;
    }
}
