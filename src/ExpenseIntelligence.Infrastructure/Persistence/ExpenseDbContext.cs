using ExpenseIntelligence.Domain.Entities;
using ExpenseIntelligence.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ExpenseIntelligence.Infrastructure.Persistence;

public class ExpenseDbContext : DbContext
{
    private readonly DatabaseOptions _databaseOptions;

    public ExpenseDbContext(
        DbContextOptions<ExpenseDbContext> options,
        IOptions<DatabaseOptions> databaseOptions) : base(options)
    {
        _databaseOptions = databaseOptions.Value;
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<CategoryDefinition> CategoryDefinitions => Set<CategoryDefinition>();

    public bool UsesLegacySchema => _databaseOptions.UseLegacySchema;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureCategoryDefinitions(modelBuilder);

        if (_databaseOptions.UseLegacySchema)
            ConfigureLegacyEntity(modelBuilder);
        else
            ConfigurePortfolioEntity(modelBuilder);
    }

    private static void ConfigureCategoryDefinitions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CategoryDefinition>(entity =>
        {
            entity.ToTable("category_definitions");
            entity.HasKey(c => c.Id);

            // Match snake_case columns from CategoryCatalogInitializer SQL
            entity.Property(c => c.Id).HasColumnName("id");
            entity.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(c => c.IsExpense).HasColumnName("is_expense");
            entity.Property(c => c.SortOrder).HasColumnName("sort_order");

            entity.HasIndex(c => new { c.Name, c.IsExpense }).IsUnique();
        });
    }

    private void ConfigureLegacyEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable(_databaseOptions.LegacyTableName);
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Id)
                .HasColumnName("change_id")
                .ValueGeneratedOnAdd();

            entity.Property(t => t.Date).HasColumnName("date");
            entity.Property(t => t.Month).HasColumnName("month").HasMaxLength(7);
            entity.Property(t => t.Description).HasColumnName("description");
            entity.Property(t => t.Amount).HasColumnName("amount");
            entity.Property(t => t.IsExpense)
                .HasColumnName("expenditure")
                .IsRequired(false);
            entity.Property(t => t.Category).HasColumnName("source");

            entity.Ignore(t => t.CategorizationSource);
            entity.Ignore(t => t.CreatedAt);
        });
    }

    private static void ConfigurePortfolioEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions");
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Id).ValueGeneratedOnAdd();
            entity.Property(t => t.Month).HasMaxLength(7).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(500).IsRequired();
            entity.Property(t => t.Category).HasMaxLength(100).IsRequired();
            entity.Property(t => t.Amount).HasPrecision(18, 2);
            entity.Property(t => t.CategorizationSource).HasMaxLength(50);
            entity.HasIndex(t => t.Date);
            entity.HasIndex(t => t.Month);
            entity.HasIndex(t => t.Category);
        });
    }
}
