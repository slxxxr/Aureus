namespace Aureus.UseCases.Validation;

internal static class InputLimits
{
    internal const int EmailMaxLength = 254;
    internal const int PasswordMaxLength = 128;

    internal const int WorkspaceNameMaxLength = 120;
    internal const int CategoryNameMaxLength = 120;
    internal const int AccountNameMaxLength = 120;

    internal const int TransactionNameMaxLength = 200;
    internal const int TransactionNoteMaxLength = 500;
    internal const long TransactionMaxAmountMinor = 100_000_000_000L;
}
