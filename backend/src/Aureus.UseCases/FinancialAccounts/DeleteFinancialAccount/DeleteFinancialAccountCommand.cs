using MediatR;

namespace Aureus.UseCases.FinancialAccounts.DeleteFinancialAccount;

public sealed record DeleteFinancialAccountCommand(Guid AccountId, Guid WorkspaceId) : IRequest;
