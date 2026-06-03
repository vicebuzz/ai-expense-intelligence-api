using ExpenseIntelligence.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExpenseIntelligence.Infrastructure.Migrations;

[DbContext(typeof(ExpenseDbContext))]
partial class ExpenseDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "7.0.20");

        modelBuilder.Entity("ExpenseIntelligence.Domain.Entities.CategoryDefinition", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("integer");

            NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

            b.Property<bool>("IsExpense").HasColumnType("boolean");
            b.Property<string>("Name").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<int>("SortOrder").HasColumnType("integer");
            b.HasKey("Id");
            b.HasIndex("Name", "IsExpense").IsUnique();
            b.ToTable("category_definitions", (string)null);
        });

        modelBuilder.Entity("ExpenseIntelligence.Domain.Entities.Transaction", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("integer");

            NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

            b.Property<decimal>("Amount").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<string>("Category").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<string>("CategorizationSource").HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<DateTime?>("CreatedAt").HasColumnType("timestamp with time zone");
            b.Property<DateOnly>("Date").HasColumnType("date");
            b.Property<string>("Month").IsRequired().HasMaxLength(7).HasColumnType("character varying(7)");
            b.Property<string>("Description").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)");
            b.Property<bool>("IsExpense").HasColumnType("boolean");
            b.HasKey("Id");
            b.HasIndex("Category");
            b.HasIndex("Date");
            b.ToTable("transactions", (string)null);
        });
    }
}
