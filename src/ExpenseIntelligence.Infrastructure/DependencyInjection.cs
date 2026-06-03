using ExpenseIntelligence.Infrastructure.Configuration;
using ExpenseIntelligence.Infrastructure.Persistence;
using ExpenseIntelligence.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ExpenseIntelligence.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var dbSection = configuration.GetSection(DatabaseOptions.SectionName);
        var databaseOptions = new DatabaseOptions
        {
            UseLegacySchema = ParseBool(dbSection["UseLegacySchema"], defaultValue: true),
            LegacyTableName = dbSection["LegacyTableName"] ?? "accountbalancemanagement"
        };

        services.AddSingleton(Options.Create(databaseOptions));

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing. " +
                "Set it in appsettings.Local.json, user secrets, or the environment variable " +
                "ConnectionStrings__DefaultConnection.");
        }

        services.AddDbContext<ExpenseDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                if (!databaseOptions.UseLegacySchema)
                {
                    npgsql.MigrationsAssembly(typeof(ExpenseDbContext).Assembly.GetName().Name);
                }
            });
        });

        services.AddScoped<CategoryCatalogService>();
        services.AddScoped<AnalyticsService>();

        return services;
    }

    private static bool ParseBool(string? value, bool defaultValue) =>
        string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : bool.TryParse(value, out var result) && result;
}
