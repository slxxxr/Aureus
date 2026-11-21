namespace Aureus.Api.Contracts.FinancialAccounts;

public sealed record ReorderFinancialAccountsRequest(
    IReadOnlyList<Guid> AccountIds,
    IReadOnlyList<Guid> ExpectedAccountIds);
