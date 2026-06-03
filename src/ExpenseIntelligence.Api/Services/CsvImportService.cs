using System.Globalization;
using ExpenseIntelligence.Domain.Entities;
using ExpenseIntelligence.Infrastructure.Persistence;
using ExpenseIntelligence.Infrastructure.Services;

namespace ExpenseIntelligence.Api.Services;

public class CsvImportResult
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class CsvImportService
{
    private static readonly string[] ExpectedHeader =
    {
        "Transaction Date",
        "Transaction Type",
        "Transaction Description",
        "Debit Amount",
        "Credit Amount",
        "Category"
    };

    private readonly ExpenseDbContext _db;
    private readonly CategoryCatalogService _categories;
    private readonly CategorizationClient _categorization;

    public CsvImportService(
        ExpenseDbContext db,
        CategoryCatalogService categories,
        CategorizationClient categorization)
    {
        _db = db;
        _categories = categories;
        _categorization = categorization;
    }

    public async Task<CsvImportResult> ImportAsync(
        Stream csvStream,
        bool autoCategorizeMissing = true,
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(csvStream);
        var result = new CsvImportResult();
        var headerLine = await reader.ReadLineAsync();

        if (headerLine is null)
        {
            result.Errors.Add("CSV file is empty.");
            return result;
        }

        var headers = ParseCsvLine(headerLine);
        var columnMap = BuildColumnMap(headers);

        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var fields = ParseCsvLine(line);
                var transaction = await MapRowAsync(fields, columnMap, autoCategorizeMissing, cancellationToken);

                if (transaction is null)
                {
                    result.Skipped++;
                    continue;
                }

                _db.Transactions.Add(transaction);
                result.Imported++;
            }
            catch (Exception ex)
            {
                result.Skipped++;
                result.Errors.Add(ex.Message);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<Transaction?> MapRowAsync(
        IReadOnlyList<string> fields,
        Dictionary<string, int> columnMap,
        bool autoCategorizeMissing,
        CancellationToken cancellationToken)
    {
        var dateRaw = GetField(fields, columnMap, "transaction date");
        var description = GetField(fields, columnMap, "transaction description");
        var debit = GetField(fields, columnMap, "debit amount");
        var credit = GetField(fields, columnMap, "credit amount");
        var category = GetField(fields, columnMap, "category");

        if (string.IsNullOrWhiteSpace(description))
            return null;

        if (!TryParseDate(dateRaw, out var date))
            throw new FormatException($"Invalid date: {dateRaw}");

        var isExpense = !string.IsNullOrWhiteSpace(debit);
        var amountRaw = isExpense ? debit : credit;

        if (!decimal.TryParse(amountRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            throw new FormatException($"Invalid amount: {amountRaw}");

        var categorizationSource = "import";

        if (string.IsNullOrWhiteSpace(category) && autoCategorizeMissing)
        {
            var suggestion = await _categorization.CategorizeAsync(description, isExpense, cancellationToken);
            category = suggestion.Category;
            categorizationSource = suggestion.Source;
        }

        category = string.IsNullOrWhiteSpace(category) ? "Other" : category.Trim();

        if (!_categories.IsValidCategory(category, isExpense))
            category = isExpense ? "Other" : "Other";

        return new Transaction
        {
            Date = date,
            Description = description.Trim(),
            Amount = amount,
            IsExpense = isExpense,
            Category = category,
            CategorizationSource = categorizationSource
        };
    }

    private static bool TryParseDate(string raw, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var parts = raw.Split('/');
        if (parts.Length == 3
            && int.TryParse(parts[0], out var day)
            && int.TryParse(parts[1], out var month)
            && int.TryParse(parts[2], out var year))
        {
            date = new DateOnly(year, month, day);
            return true;
        }

        return DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static Dictionary<string, int> BuildColumnMap(IReadOnlyList<string> headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++)
            map[headers[i].Trim()] = i;
        return map;
    }

    private static string GetField(IReadOnlyList<string> fields, Dictionary<string, int> map, string name)
    {
        if (!map.TryGetValue(name, out var index) || index >= fields.Count)
            return string.Empty;
        return fields[index].Trim();
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = "";
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                fields.Add(current);
                current = "";
                continue;
            }

            current += ch;
        }

        fields.Add(current);
        return fields;
    }
}
