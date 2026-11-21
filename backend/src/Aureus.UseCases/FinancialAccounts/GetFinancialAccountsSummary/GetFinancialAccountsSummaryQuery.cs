using MediatR;

namespace Aureus.UseCases.FinancialAccounts.GetFinancialAccountsSummary;

public sealed record GetFinancialAccountsSummaryQuery(Guid WorkspaceId) : IRequest<FinancialAccountsSummary?>;
