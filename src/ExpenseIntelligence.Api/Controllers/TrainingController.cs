using ExpenseIntelligence.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrainingController : ControllerBase
{
    private readonly TransactionCategorizationService _categorization;

    public TrainingController(TransactionCategorizationService categorization) =>
        _categorization = categorization;

    /// <summary>
    /// Push labelled rows from the database to the ML service (user catalog categories only).
    /// </summary>
    [HttpPost("sync-ml")]
    public async Task<IActionResult> SyncMlFromDatabase(CancellationToken cancellationToken)
    {
        var sampleCount = await _categorization.RetrainFromDatabaseAsync(cancellationToken);
        return Ok(new { sampleCount });
    }
}
