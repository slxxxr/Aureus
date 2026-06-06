using Aureus.Domain.Transactions;

namespace Aureus.Api.Contracts.Categories;

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    TransactionType Type,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
