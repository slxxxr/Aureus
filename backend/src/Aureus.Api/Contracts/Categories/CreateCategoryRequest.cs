using Aureus.Domain.Transactions;

namespace Aureus.Api.Contracts.Categories;

public sealed record CreateCategoryRequest(
    string Name,
    TransactionType Type);
