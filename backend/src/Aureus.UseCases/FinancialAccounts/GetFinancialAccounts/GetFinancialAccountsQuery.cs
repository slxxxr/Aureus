using Aureus.Domain.FinancialAccounts;
using MediatR;

namespace Aureus.UseCases.FinancialAccounts.GetFinancialAccounts;

public sealed record GetFinancialAccountsQuery(Guid WorkspaceId) : IRequest<IReadOnlyList<FinancialAccount>>;
