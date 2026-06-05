using Aureus.Domain.FinancialAccounts;
using MediatR;

namespace Aureus.UseCases.FinancialAccounts.CreateFinancialAccount;

public sealed record CreateFinancialAccountCommand(
    Guid WorkspaceId,
    string Name,
    string Currency,
    long InitialBalanceMinor) : IRequest<FinancialAccount>;
