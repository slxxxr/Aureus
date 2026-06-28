using MediatR;

namespace Aureus.UseCases.Transactions.ImportTransactions;

public sealed record ImportRowPreview(
    int RowNumber,
    bool IsValid,
    string? ErrorCode,
    string? ErrorSubject,
    string Date,
    string Type,
    string Amount,
    string Account,
    string Category,
    string Name,
    string Note);

public sealed record ImportPreviewResult(
    IReadOnlyList<ImportRowPreview> Rows,
    int ValidCount,
    int ErrorCount);

public sealed record ImportPreviewQuery(
    Guid WorkspaceId,
    byte[] FileContent) : IRequest<ImportPreviewResult>;
