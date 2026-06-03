using ExpenseIntelligence.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategorizationController : ControllerBase
{
    private readonly CategorizationClient _client;

    public CategorizationController(CategorizationClient client) => _client = client;

    [HttpPost]
    public async Task<IActionResult> Categorize(
        [FromBody] CategorizeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _client.CategorizeAsync(
            request.Description,
            request.IsExpense,
            cancellationToken);
        return Ok(result);
    }

    public record CategorizeRequest(string Description, bool IsExpense);
}
