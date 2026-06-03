using ExpenseIntelligence.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportController : ControllerBase
{
    private readonly CsvImportService _importService;

    public ImportController(CsvImportService importService) => _importService = importService;

    /// <summary>
    /// Upload a UK-style bank CSV (see seed/transactions.demo.csv for format).
    /// </summary>
    [HttpPost("csv")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<CsvImportResult>> UploadCsv(
        IFormFile file,
        [FromQuery] bool autoCategorize = true,
        CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
            return BadRequest("File is empty.");

        await using var stream = file.OpenReadStream();
        var result = await _importService.ImportAsync(stream, autoCategorize, cancellationToken);
        return Ok(result);
    }
}
