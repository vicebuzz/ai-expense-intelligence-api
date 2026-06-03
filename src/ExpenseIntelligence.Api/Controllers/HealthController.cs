using ExpenseIntelligence.Infrastructure.Configuration;
using ExpenseIntelligence.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ExpenseIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ExpenseDbContext _db;
    private readonly DatabaseOptions _dbOptions;
    private readonly IConfiguration _configuration;

    public HealthController(
        ExpenseDbContext db,
        IOptions<DatabaseOptions> dbOptions,
        IConfiguration configuration)
    {
        _db = db;
        _dbOptions = dbOptions.Value;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
        if (!canConnect)
        {
            return StatusCode(503, new
            {
                status = "unhealthy",
                database = "disconnected"
            });
        }

        var count = await _db.Transactions.CountAsync(cancellationToken);
        var connection = _configuration.GetConnectionString("DefaultConnection") ?? "";
        var databaseName = TryGetDatabaseName(connection);

        return Ok(new
        {
            status = "healthy",
            service = "ExpenseIntelligence.Api",
            database = databaseName,
            schema = _dbOptions.UseLegacySchema ? "legacy" : "portfolio",
            table = _dbOptions.UseLegacySchema ? _dbOptions.LegacyTableName : "transactions",
            transactionCount = count
        });
    }

    private static string? TryGetDatabaseName(string connectionString)
    {
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0].Trim().Equals("Database", StringComparison.OrdinalIgnoreCase))
                return kv[1].Trim();
        }

        return null;
    }
}
