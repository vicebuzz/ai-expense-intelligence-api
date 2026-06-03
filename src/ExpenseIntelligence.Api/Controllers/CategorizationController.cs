using ExpenseIntelligence.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategorizationController : ControllerBase
{
    private readonly TransactionCategorizationService _categorization;

    public CategorizationController(TransactionCategorizationService categorization) =>
        _categorization = categorization;

    [HttpPost]
    public async Task<IActionResult> Categorize(
        [FromBody] CategorizeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _categorization.CategorizeAsync(
            request.Description,
            request.IsExpense,
            cancellationToken);
        return Ok(result);
    }

    public record CategorizeRequest(string Description, bool IsExpense);
}
