using Aureus.Domain.FinancialAccounts;
using MediatR;

namespace Aureus.UseCases.FinancialAccounts.UpdateFinancialAccount;

public sealed record UpdateFinancialAccountCommand(
    Guid AccountId,
    Guid WorkspaceId,
    string? Name,
    long? InitialBalanceMinor) : IRequest<FinancialAccount>;
