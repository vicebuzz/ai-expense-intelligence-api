using ExpenseIntelligence.Infrastructure.Configuration;
using ExpenseIntelligence.Infrastructure.Persistence;
using ExpenseIntelligence.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseIntelligence.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing. " +
                "Set it in appsettings.Local.json, user secrets, or the environment variable " +
                "ConnectionStrings__DefaultConnection.");
        }

        var useLegacy = configuration.GetValue<bool>($"{DatabaseOptions.SectionName}:UseLegacySchema");

        services.AddDbContext<ExpenseDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                if (!useLegacy)
                {
                    npgsql.MigrationsAssembly(typeof(ExpenseDbContext).Assembly.GetName().Name);
                }
            });
        });

        services.AddSingleton<CategoryCatalogService>();
        services.AddScoped<AnalyticsService>();

        return services;
    }
}
