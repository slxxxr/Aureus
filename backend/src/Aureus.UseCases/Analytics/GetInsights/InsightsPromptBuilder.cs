using System.Globalization;
using System.Text;
using Aureus.Domain.Analytics;
using Aureus.Domain.Transactions;

namespace Aureus.UseCases.Analytics.GetInsights;

internal static class InsightsPromptBuilder
{
    public static string Build(
        GetInsightsQuery query,
        Tier tier,
        TimeInterval interval,
        int transactionCount,
        FinancialContext ctx)
    {
        var sb = new StringBuilder();

        sb.Append("""
            You are a personal finance assistant. Answer the user's question using the financial data provided below.

            Rules:
            - Ground every claim in the data. Do not invent figures, transactions, or categories that are not present.
            - Answer only what is asked. Do not add unsolicited advice, observations, or recommendations.
            - If the question explicitly asks for suggestions, recommendations, or analysis, you may draw reasonable conclusions from the data — frame them as suggestions, not certainties.
            - If the data does not fully cover what is asked, say briefly what is missing, then offer what you can from what is available.
            - Write all amounts using a space as thousands separator and a dot as decimal separator: 10 000.00 RUB. Never use a comma as decimal separator.
            - Be concise. Use bullet points for lists.

            """);

        sb.AppendLine("## Financial data");
        sb.AppendLine(FormatPeriodLine(query.From, query.To));
        sb.AppendLine($"Total transactions in period: {transactionCount}");
        sb.AppendLine();

        AppendSummary(sb, ctx.Summary);
        AppendCategorySection(sb, "Expenses by category", ctx.ExpenseCategories);
        AppendCategorySection(sb, "Income by category", ctx.IncomeCategories);

        switch (tier)
        {
            case Tier.Small:
                AppendAllTransactions(sb, ctx.Transactions);
                break;
            case Tier.Medium:
                AppendTimeSeries(sb, ctx.TimeSeries, interval);
                AppendCategoryMatrix(sb, ctx.Transactions, interval);
                break;
            case Tier.Large:
                AppendTimeSeries(sb, ctx.TimeSeries, interval);
                AppendTopNames(sb, ctx.TopNames);
                break;
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine($"Respond in {query.Language}.");
        sb.AppendLine("If the question below is not about personal finances, spending, saving, income, or budgeting — do not answer it under any circumstances. Reply only that you assist exclusively with financial questions.");
        sb.AppendLine($"Question: {query.Question}");

        return sb.ToString();
    }

    private static string FormatPeriodLine(DateOnly? from, DateOnly? to) =>
        (from, to) switch
        {
            (not null, not null) =>
                $"Period: {from:yyyy-MM-dd} to {to:yyyy-MM-dd} ({to.Value.DayNumber - from.Value.DayNumber} days).",
            (not null, null) => $"Period: from {from:yyyy-MM-dd} (no end date).",
            (null, not null) => $"Period: up to {to:yyyy-MM-dd} (no start date).",
            _ => "Period: all time.",
        };

    private static void AppendSummary(StringBuilder sb, IReadOnlyList<CurrencySummary> summary)
    {
        sb.AppendLine("### Summary");

        if (summary.Count == 0)
        {
            sb.AppendLine("No transactions in this period.");
            return;
        }

        foreach (var s in summary)
        {
            sb.AppendLine($"Currency: {s.Currency}");
            sb.AppendLine($"  Total income:  {Fmt(s.IncomeMinor)}");
            sb.AppendLine($"  Total expense: {Fmt(s.ExpensesMinor)}");
            sb.AppendLine($"  Net:           {Fmt(s.NetMinor)}");
        }
    }

    private static void AppendCategorySection(StringBuilder sb, string title, IReadOnlyList<BreakdownItem> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine($"### {title}");
        foreach (var c in items.OrderByDescending(x => x.AmountMinor))
        {
            sb.AppendLine($"  {c.Label ?? "Uncategorized"}: {Fmt(c.AmountMinor)} {c.Currency}");
        }
    }

    private static void AppendAllTransactions(StringBuilder sb, IReadOnlyList<TransactionContext> transactions)
    {
        if (transactions.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("### All transactions");
        foreach (var t in transactions)
        {
            var type = t.Type == TransactionType.Income ? "income" : "expense";
            sb.AppendLine(
                $"  {t.OccurredAt:yyyy-MM-dd}  {t.Name}  [{t.CategoryLabel ?? "Uncategorized"}]  {type}  {Fmt(t.AmountMinor)} {t.Currency}");
        }
    }

    private static void AppendCategoryMatrix(
        StringBuilder sb, IReadOnlyList<TransactionContext> transactions, TimeInterval interval)
    {
        if (transactions.Count == 0)
        {
            return;
        }

        var intervalLabel = interval switch
        {
            TimeInterval.Day  => "day",
            TimeInterval.Week => "week of",
            _                 => "month",
        };

        var byCategoryAndPeriod = transactions
            .GroupBy(t => (
                Type: t.Type,
                Category: t.CategoryLabel ?? "Uncategorized",
                t.Currency,
                Period: BucketStart(t.OccurredAt, interval)))
            .Select(g => (
                g.Key.Type,
                g.Key.Category,
                g.Key.Currency,
                g.Key.Period,
                Total: g.Sum(t => t.AmountMinor),
                Top3: g.OrderByDescending(t => t.AmountMinor).Take(3).ToList()))
            .ToList();

        var byCategory = byCategoryAndPeriod
            .GroupBy(r => (r.Type, r.Category, r.Currency))
            .OrderBy(g => g.Key.Type == TransactionType.Expense ? 0 : 1)
            .ThenBy(g => g.Key.Category);

        sb.AppendLine();
        sb.AppendLine($"### Category breakdown by {interval.ToString().ToLower()}");

        foreach (var cat in byCategory)
        {
            var typeLabel = cat.Key.Type == TransactionType.Expense ? "expense" : "income";
            sb.AppendLine($"[{typeLabel}] {cat.Key.Category} ({cat.Key.Currency})");
            foreach (var row in cat.OrderBy(r => r.Period))
            {
                var top3 = string.Join(" · ", row.Top3.Select(t => $"{t.Name} {Fmt(t.AmountMinor)}"));
                sb.AppendLine($"  {intervalLabel} {row.Period:yyyy-MM-dd}: {Fmt(row.Total)} | top: {top3}");
            }
        }
    }

    private static void AppendTimeSeries(StringBuilder sb, IReadOnlyList<TimeSeriesPoint> timeSeries, TimeInterval interval)
    {
        if (timeSeries.Count == 0)
        {
            return;
        }

        var label = interval switch
        {
            TimeInterval.Day  => "Daily",
            TimeInterval.Week => "Weekly",
            _                 => "Monthly",
        };

        sb.AppendLine();
        sb.AppendLine($"### {label} income/expense totals");

        var byCurrency = timeSeries.GroupBy(p => p.Currency).ToList();
        var multiCurrency = byCurrency.Count > 1;

        foreach (var g in byCurrency)
        {
            if (multiCurrency)
            {
                sb.AppendLine($"Currency: {g.Key}");
            }

            foreach (var p in g.OrderBy(p => p.PeriodStart))
            {
                var periodKey = interval switch
                {
                    TimeInterval.Day  => $"{p.PeriodStart:yyyy-MM-dd}",
                    TimeInterval.Week => $"week of {p.PeriodStart:yyyy-MM-dd}",
                    _                 => $"{p.PeriodStart:yyyy-MM}",
                };
                sb.AppendLine($"  {periodKey}  income: {Fmt(p.IncomeMinor)}  expense: {Fmt(p.ExpensesMinor)}");
            }
        }
    }

    private static void AppendTopNames(StringBuilder sb, IReadOnlyList<BreakdownItem> names)
    {
        if (names.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine($"### Top {names.Count} transaction names (aggregated totals for the full period)");
        foreach (var n in names)
        {
            sb.AppendLine($"  {n.Label}: {Fmt(n.AmountMinor)} {n.Currency}");
        }
    }

    private static string Fmt(long minor) =>
        (minor / 100m).ToString("F2", CultureInfo.InvariantCulture);

    private static DateOnly BucketStart(DateOnly day, TimeInterval interval) => interval switch
    {
        TimeInterval.Week  => day.AddDays(-(((int)day.DayOfWeek + 6) % 7)),
        TimeInterval.Month => new DateOnly(day.Year, day.Month, 1),
        _                  => day,
    };
}
