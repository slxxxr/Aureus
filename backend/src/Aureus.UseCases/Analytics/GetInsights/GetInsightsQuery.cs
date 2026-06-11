using MediatR;

namespace Aureus.UseCases.Analytics.GetInsights;

public sealed record GetInsightsQuery(
    Guid WorkspaceId,
    string Question,
    DateOnly? From,
    DateOnly? To,
    string Language) : IRequest<string>;
