namespace Aureus.Api.Contracts.FinancialAccounts;

public sealed record UpdateFinancialAccountRequest(
    string? Name,
    long? InitialBalanceMinor);
