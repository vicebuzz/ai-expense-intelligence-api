using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExpenseIntelligence.Infrastructure.Migrations;

[Migration("20250603000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "transactions",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                Month = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                IsExpense = table.Column<bool>(type: "boolean", nullable: false),
                Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                CategorizationSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_transactions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_transactions_Category",
            table: "transactions",
            column: "Category");

        migrationBuilder.CreateIndex(
            name: "IX_transactions_Date",
            table: "transactions",
            column: "Date");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "transactions");
    }
}
