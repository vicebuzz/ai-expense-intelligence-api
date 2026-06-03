using ExpenseIntelligence.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly AnalyticsService _analytics;

    public AnalyticsController(AnalyticsService analytics) => _analytics = analytics;

    [HttpGet("months")]
    public async Task<IActionResult> Months(CancellationToken cancellationToken) =>
        Ok(await _analytics.GetAvailableMonthsAsync(cancellationToken));

    [HttpGet("expense-categories")]
    public async Task<IActionResult> ExpenseCategories(CancellationToken cancellationToken) =>
        Ok(await _analytics.GetExpenseCategoriesAsync(cancellationToken));

    [HttpGet("spend-by-category")]
    public async Task<IActionResult> SpendByCategory(
        [FromQuery] string? month,
        CancellationToken cancellationToken) =>
        Ok(await _analytics.GetSpendByCategoryAsync(month, cancellationToken));

    [HttpGet("budget-502030/actual")]
    public async Task<IActionResult> Budget502030Actual(
        [FromQuery] string? month,
        CancellationToken cancellationToken) =>
        Ok(await _analytics.Get502030ActualAsync(month, cancellationToken));

    [HttpGet("budget-502030/target")]
    public async Task<IActionResult> Budget502030Target(
        [FromQuery] string? month,
        CancellationToken cancellationToken) =>
        Ok(await _analytics.Get502030TargetAsync(month, cancellationToken));

    [HttpGet("savings/latest")]
    public async Task<IActionResult> LatestSavings(CancellationToken cancellationToken) =>
        Ok(new { amount = await _analytics.GetLatestSavingsAsync(cancellationToken) });

    [HttpGet("daily-expenditures")]
    public async Task<IActionResult> DailyExpenditures(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken) =>
        Ok(await _analytics.GetDailyExpendituresAsync(from, to, cancellationToken));

    [HttpGet("monthly-salary")]
    public async Task<IActionResult> MonthlySalary(CancellationToken cancellationToken) =>
        Ok(await _analytics.GetMonthlySalaryAsync(cancellationToken));

    [HttpGet("earnings-vs-expenditures")]
    public async Task<IActionResult> EarningsVsExpenditures(CancellationToken cancellationToken) =>
        Ok(await _analytics.GetEarningsVsExpendituresByMonthAsync(cancellationToken));

    [HttpGet("category-pivot")]
    public async Task<IActionResult> CategoryPivot(
        [FromQuery] string metric = "amount",
        CancellationToken cancellationToken = default) =>
        Ok(await _analytics.GetCategoryPivotAsync(metric, cancellationToken));

    [HttpGet("category-month-table")]
    public async Task<IActionResult> CategoryMonthTable(CancellationToken cancellationToken) =>
        Ok(await _analytics.GetCategoryMonthTableAsync(cancellationToken));

    [HttpGet("expenses-vs-earnings")]
    public async Task<IActionResult> ExpensesVsEarnings(CancellationToken cancellationToken) =>
        Ok(await _analytics.GetExpensesVsEarningsTotalsAsync(cancellationToken));

    [HttpGet("monthly-by-categories")]
    public async Task<IActionResult> MonthlyByCategories(
        [FromQuery] string categories,
        CancellationToken cancellationToken)
    {
        var list = string.IsNullOrWhiteSpace(categories)
            ? Array.Empty<string>()
            : categories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return Ok(await _analytics.GetMonthlySpendForCategoriesAsync(list, cancellationToken));
    }

    [HttpGet("spending-by-category")]
    public async Task<IActionResult> SpendingByCategory(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken) =>
        Ok(await _analytics.GetSpendingByCategoryAsync(from, to, cancellationToken));

    [HttpGet("average-monthly-by-category")]
    public async Task<IActionResult> AverageMonthlyByCategory(CancellationToken cancellationToken) =>
        Ok(await _analytics.GetAverageMonthlySpendByCategoryAsync(cancellationToken));

    [HttpGet("top-merchants")]
    public async Task<IActionResult> TopMerchants(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await _analytics.GetTopMerchantsAsync(limit, cancellationToken));
}
