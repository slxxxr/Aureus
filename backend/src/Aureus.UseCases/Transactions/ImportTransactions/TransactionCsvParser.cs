using System.Globalization;
using System.Text;
using Aureus.Domain.Categories;
using Aureus.Domain.FinancialAccounts;
using Aureus.Domain.Transactions;
using Aureus.UseCases.Validation;
using CsvHelper;
using CsvHelper.Configuration;

namespace Aureus.UseCases.Transactions.ImportTransactions;

internal static class TransactionCsvParser
{
    internal const int MaxRows = 5_000;
    internal const long MaxFileSizeBytes = 5 * 1024 * 1024;

    internal static List<ParsedRow> Parse(
        byte[] content,
        IReadOnlyList<FinancialAccount> accounts,
        IReadOnlyList<Category> categories)
    {
        var accountByName = accounts.ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase);
        var categoryByKey = categories.ToDictionary(
            c => (c.Name.ToLowerInvariant(), c.Type),
            c => c);

        var delimiter = DetectDelimiter(content);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter.ToString(),
            PrepareHeaderForMatch = args => args.Header.ToLowerInvariant().Trim(),
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null,
        };

        using var stream = new MemoryStream(content);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var csv = new CsvReader(reader, config);

        var results = new List<ParsedRow>();
        int rowNumber = 0;

        foreach (var row in csv.GetRecords<TransactionImportRow>())
        {
            rowNumber++;
            results.Add(ValidateRow(rowNumber, row, accountByName, categoryByKey));
        }

        return results;
    }

    private static ParsedRow ValidateRow(
        int rowNumber,
        TransactionImportRow row,
        Dictionary<string, FinancialAccount> accountByName,
        Dictionary<(string, TransactionType), Category> categoryByKey)
    {
        var displayNote = row.Note ?? string.Empty;

        if (!DateOnly.TryParseExact(row.Date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return Invalid(rowNumber, row, ImportRowErrorCode.InvalidDate);
        }

        if (!Enum.TryParse<TransactionType>(row.Type.Trim(), ignoreCase: true, out var type))
        {
            return Invalid(rowNumber, row, ImportRowErrorCode.InvalidType);
        }

        var amountStr = row.Amount.Trim().Replace(',', '.');
        if (!decimal.TryParse(amountStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            return Invalid(rowNumber, row, ImportRowErrorCode.InvalidAmount);
        }

        var amountMinor = (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
        if (amountMinor > InputLimits.TransactionMaxAmountMinor)
        {
            return Invalid(rowNumber, row, ImportRowErrorCode.AmountTooLarge);
        }

        var accountName = row.Account.Trim();
        if (string.IsNullOrEmpty(accountName) || !accountByName.TryGetValue(accountName, out var account))
        {
            return Invalid(rowNumber, row, ImportRowErrorCode.AccountNotFound, accountName);
        }

        var categoryName = row.Category.Trim();
        if (string.IsNullOrEmpty(categoryName) || !categoryByKey.TryGetValue((categoryName.ToLowerInvariant(), type), out var category))
        {
            return Invalid(rowNumber, row, ImportRowErrorCode.CategoryNotFound, categoryName);
        }

        var name = row.Name.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return Invalid(rowNumber, row, ImportRowErrorCode.NameRequired);
        }

        if (name.Length > InputLimits.TransactionNameMaxLength)
        {
            return Invalid(rowNumber, row, ImportRowErrorCode.NameTooLong);
        }

        var note = string.IsNullOrWhiteSpace(row.Note) ? null : row.Note.Trim();
        if (note is not null && note.Length > InputLimits.TransactionNoteMaxLength)
        {
            return Invalid(rowNumber, row, ImportRowErrorCode.NoteTooLong);
        }

        var valid = new ValidImportRow(date, type, amountMinor, account, category, name, note);
        return new ParsedRow(rowNumber, row.Date, row.Type, row.Amount, row.Account, row.Category, name, displayNote, valid, null, null);
    }

    private static ParsedRow Invalid(int rowNumber, TransactionImportRow row, string errorCode, string? errorSubject = null) =>
        new(rowNumber, row.Date, row.Type, row.Amount, row.Account, row.Category, row.Name, row.Note ?? string.Empty, null, errorCode, errorSubject);

    private static char DetectDelimiter(byte[] content)
    {
        using var reader = new StreamReader(new MemoryStream(content), Encoding.UTF8);
        var firstLine = reader.ReadLine() ?? string.Empty;
        return firstLine.Contains(';') ? ';' : ',';
    }
}
