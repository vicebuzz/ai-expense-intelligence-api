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
    /// UK bank export CSV (same columns as grafana-expenses csv/).
    /// Category column optional — empty cells are classified via transformer ML.
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

    [HttpGet("template")]
    public IActionResult DownloadTemplate()
    {
        const string header =
            "Transaction Date,Transaction Type,Sort Code,Account Number,Transaction Description,Debit Amount,Credit Amount,Balance,Category";

        return File(
            System.Text.Encoding.UTF8.GetBytes(header + "\n"),
            "text/csv",
            "bank-export-template.csv");
    }
}
