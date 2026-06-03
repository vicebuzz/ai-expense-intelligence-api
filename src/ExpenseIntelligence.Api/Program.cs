using ExpenseIntelligence.Api;
using ExpenseIntelligence.Api.Services;
using ExpenseIntelligence.Infrastructure;
using ExpenseIntelligence.Infrastructure.Configuration;
using ExpenseIntelligence.Infrastructure.Persistence;
using ExpenseIntelligence.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Optional local overrides (gitignored) — copy from appsettings.Local.json.example
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "AI Expense Intelligence API", Version = "v1" });
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<CsvImportService>();
builder.Services.AddScoped<TransactionCategorizationService>();

var categorizationBaseUrl = builder.Configuration["CategorizationService:BaseUrl"]
    ?? "http://localhost:8000";

builder.Services.AddHttpClient<CategorizationClient>(client =>
{
    client.BaseAddress = new Uri(categorizationBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Dashboard", policy =>
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                ?? new[] { "http://localhost:5173" })
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();
    var dbOptions = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Database");

    if (!await db.Database.CanConnectAsync())
    {
        throw new InvalidOperationException(
            "Cannot connect to PostgreSQL. Check ConnectionStrings:DefaultConnection " +
            "(appsettings.Local.json or ConnectionStrings__DefaultConnection).");
    }

    var catalog = scope.ServiceProvider.GetRequiredService<CategoryCatalogService>();

    if (dbOptions.UseLegacySchema)
    {
        var count = await db.Transactions.CountAsync();
        logger.LogInformation(
            "Connected to legacy table {Table}. {Count} transactions loaded.",
            dbOptions.LegacyTableName,
            count);
    }
    else
    {
        await db.Database.MigrateAsync();
        await DbSeeder.SeedIfEmptyAsync(db);
        logger.LogInformation("Portfolio schema migrated and seeded if empty.");
    }

    await CategoryCatalogInitializer.InitializeAsync(db, catalog, logger);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Dashboard");
app.UseAuthorization();
app.MapControllers();

app.Run();
