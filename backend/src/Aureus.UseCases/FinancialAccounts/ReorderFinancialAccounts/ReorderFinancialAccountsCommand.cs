using MediatR;

namespace Aureus.UseCases.FinancialAccounts.ReorderFinancialAccounts;

public sealed record ReorderFinancialAccountsCommand(
    Guid WorkspaceId,
    IReadOnlyList<Guid> AccountIds,
    IReadOnlyList<Guid> ExpectedAccountIds) : IRequest;
