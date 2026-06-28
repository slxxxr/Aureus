namespace Aureus.Api.Contracts.Transactions;

public sealed record ImportRowPreviewResponse(
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

public sealed record ImportPreviewResponse(
    IReadOnlyList<ImportRowPreviewResponse> Rows,
    int ValidCount,
    int ErrorCount);
