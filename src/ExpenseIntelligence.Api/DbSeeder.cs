using System.Globalization;
using ExpenseIntelligence.Domain.Entities;
using ExpenseIntelligence.Infrastructure.Persistence;

namespace ExpenseIntelligence.Api;

public static class DbSeeder
{
    public static async Task SeedIfEmptyAsync(ExpenseDbContext db)
    {
        if (await db.Transactions.AnyAsync())
            return;

        var seedPath = ResolveSeedPath("transactions.demo.csv");
        if (!File.Exists(seedPath))
            return;

        var lines = await File.ReadAllLinesAsync(seedPath);
        if (lines.Length < 2)
            return;

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var fields = line.Split(',');
            if (fields.Length < 6)
                continue;

            var date = DateOnly.ParseExact(fields[0], "dd/MM/yyyy", CultureInfo.InvariantCulture);
            var description = fields[2].Trim();
            var debit = fields[3].Trim();
            var credit = fields[4].Trim();
            var category = fields[5].Trim();
            var isExpense = !string.IsNullOrEmpty(debit);
            var amount = decimal.Parse(isExpense ? debit : credit, CultureInfo.InvariantCulture);

            db.Transactions.Add(new Transaction
            {
                Date = date,
                Description = description,
                Amount = amount,
                IsExpense = isExpense,
                Category = category,
                CategorizationSource = "seed"
            });
        }

        await db.SaveChangesAsync();
    }

    private static string ResolveSeedPath(string fileName)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "seed", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "seed", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "seed", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "seed", fileName),
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        return "";
    }
}
