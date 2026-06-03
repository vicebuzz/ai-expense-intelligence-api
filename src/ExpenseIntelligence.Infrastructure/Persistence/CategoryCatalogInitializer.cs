using ExpenseIntelligence.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseIntelligence.Infrastructure.Persistence;

public static class CategoryCatalogInitializer
{
    public static async Task InitializeAsync(
        ExpenseDbContext db,
        CategoryCatalogService catalog,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS category_definitions (
                id SERIAL PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                is_expense BOOLEAN NOT NULL,
                sort_order INT NOT NULL DEFAULT 0,
                CONSTRAINT uq_category_definitions_name_type UNIQUE (name, is_expense)
            );
            """,
            cancellationToken);

        await catalog.EnsureSeededAsync(cancellationToken);

        var count = await db.CategoryDefinitions.CountAsync(cancellationToken);
        logger.LogInformation("Category catalog ready ({Count} definitions).", count);
    }
}
