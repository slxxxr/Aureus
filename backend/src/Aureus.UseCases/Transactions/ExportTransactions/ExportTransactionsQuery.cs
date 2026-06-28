using Aureus.Domain.Analytics;
using MediatR;

namespace Aureus.UseCases.Transactions.ExportTransactions;

public sealed record ExportTransactionsQuery(AnalyticsFilter Filter) : IRequest<byte[]>;
