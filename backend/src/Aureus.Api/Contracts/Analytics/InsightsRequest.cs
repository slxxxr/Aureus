namespace Aureus.Api.Contracts.Analytics;

public sealed record InsightsRequest(string Question, DateOnly? From, DateOnly? To, string Language = "Russian");
