namespace Aureus.Api.Contracts.FinancialAccounts;

public sealed record FinancialAccountResponse(
    Guid Id,
    string Name,
    string Currency,
    long InitialBalanceMinor,
    long CurrentBalanceMinor,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
