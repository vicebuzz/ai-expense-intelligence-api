using System.Globalization;
using ExpenseIntelligence.Domain.Entities;
using ExpenseIntelligence.Infrastructure.Persistence;
using ExpenseIntelligence.Infrastructure.Services;

namespace ExpenseIntelligence.Api.Services;

public class CsvImportResult
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public int CategorizedByMl { get; set; }
    public int UsedCsvCategory { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class CsvImportService
{
    private readonly ExpenseDbContext _db;
    private readonly TransactionCategorizationService _categorization;

    public CsvImportService(
        ExpenseDbContext db,
        TransactionCategorizationService categorization)
    {
        _db = db;
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
        var pendingRows = new List<(List<string> Fields, Dictionary<string, int> Map)>();

        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                pendingRows.Add((ParseCsvLine(line), columnMap));
            }
            catch (Exception ex)
            {
                result.Skipped++;
                result.Errors.Add(ex.Message);
            }
        }

        var needsMl = new List<(int Index, string Description, bool IsExpense)>();
        var parsed = new List<ParsedRow?>(pendingRows.Count);

        for (var i = 0; i < pendingRows.Count; i++)
        {
            var (fields, map) = pendingRows[i];
            try
            {
                var row = ParseFields(fields, map);
                if (row is null)
                {
                    parsed.Add(null);
                    result.Skipped++;
                    continue;
                }

                parsed.Add(row);

                if (autoCategorizeMissing && string.IsNullOrWhiteSpace(row.CsvCategory))
                    needsMl.Add((i, row.Description, row.IsExpense));
            }
            catch (Exception ex)
            {
                parsed.Add(null);
                result.Skipped++;
                result.Errors.Add(ex.Message);
            }
        }

        var mlResults = needsMl.Count > 0
            ? await _categorization.CategorizeBatchAsync(
                needsMl.Select(x => (x.Description, x.IsExpense)).ToList(),
                cancellationToken)
            : new List<CategorizationSuggestion>();

        for (var m = 0; m < needsMl.Count; m++)
        {
            var (index, _, _) = needsMl[m];
            var suggestion = mlResults[m];
            var row = parsed[index]!;
            row.Category = suggestion.Category;
            row.CategorizationSource = suggestion.Source;
            result.CategorizedByMl++;
        }

        foreach (var row in parsed)
        {
            if (row is null)
                continue;

            if (!string.IsNullOrWhiteSpace(row.CsvCategory))
            {
                row.Category = row.CsvCategory.Trim();
                row.CategorizationSource = "csv";
                result.UsedCsvCategory++;
            }

            row.Category = NormalizeCategory(row.Category, row.IsExpense);

            if (!CategoryFilter.IsIncluded(row.Category))
            {
                result.Skipped++;
                result.Errors.Add($"Skipped (category '{CategoryFilter.Excluded}'): {row.Description}");
                continue;
            }

            _db.Transactions.Add(new Transaction
            {
                Date = row.Date,
                Month = row.Month,
                Description = row.Description,
                Amount = row.Amount,
                IsExpense = row.IsExpense,
                Category = row.Category,
                CategorizationSource = row.CategorizationSource
            });
            result.Imported++;
        }

        await _db.SaveChangesAsync(cancellationToken);

        if (result.Imported > 0)
            await _categorization.RetrainFromDatabaseAsync(cancellationToken);

        return result;
    }

    private static string NormalizeCategory(string category, bool isExpense)
    {
        if (string.IsNullOrWhiteSpace(category))
            return CategoryFilter.Excluded;

        return category.Trim();
    }

    private sealed class ParsedRow
    {
        public DateOnly Date { get; init; }
        public string Month { get; init; } = "";
        public string Description { get; init; } = "";
        public decimal Amount { get; init; }
        public bool IsExpense { get; init; }
        public string CsvCategory { get; init; } = "";
        public string Category { get; set; } = "";
        public string CategorizationSource { get; set; } = "import";
    }

    private static ParsedRow? ParseFields(
        IReadOnlyList<string> fields,
        Dictionary<string, int> columnMap)
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

        return new ParsedRow
        {
            Date = date,
            Month = Transaction.MonthFromDate(date),
            Description = description.Trim(),
            Amount = amount,
            IsExpense = isExpense,
            CsvCategory = category
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

    public static List<string> ParseCsvLine(string line)
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
