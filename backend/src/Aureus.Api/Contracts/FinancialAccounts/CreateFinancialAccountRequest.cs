namespace Aureus.Api.Contracts.FinancialAccounts;

public sealed record CreateFinancialAccountRequest(
    string Name,
    string Currency,
    long InitialBalanceMinor);
