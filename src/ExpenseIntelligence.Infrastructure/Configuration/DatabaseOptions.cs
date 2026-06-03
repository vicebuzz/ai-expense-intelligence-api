namespace ExpenseIntelligence.Infrastructure.Configuration;

public class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// When true, maps to the existing AccountBalanceManagement table (grafana-expenses schema).
    /// When false, uses the portfolio transactions table and EF migrations.
    /// </summary>
    public bool UseLegacySchema { get; set; } = true;

    /// <summary>
    /// PostgreSQL table name. Use accountbalancemanagement (default for unquoted psycopg2 inserts)
    /// or AccountBalanceManagement if the table was created with quoted identifiers.
    /// </summary>
    public string LegacyTableName { get; set; } = "accountbalancemanagement";
}
