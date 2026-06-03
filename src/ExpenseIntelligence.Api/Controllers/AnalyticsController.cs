using ExpenseIntelligence.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly AnalyticsService _analytics;

    public AnalyticsController(AnalyticsService analytics) => _analytics = analytics;

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
