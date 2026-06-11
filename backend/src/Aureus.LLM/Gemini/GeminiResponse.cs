using System.Text.Json.Serialization;

namespace Aureus.LLM.Gemini;

internal sealed record GeminiResponse(
    [property: JsonPropertyName("candidates")] List<GeminiCandidate>? Candidates,
    [property: JsonPropertyName("usageMetadata")] GeminiUsageMetadata? UsageMetadata);

internal sealed record GeminiCandidate(
    [property: JsonPropertyName("content")] GeminiContent Content);

internal sealed record GeminiUsageMetadata(
    [property: JsonPropertyName("promptTokenCount")] int PromptTokenCount,
    [property: JsonPropertyName("candidatesTokenCount")] int CandidatesTokenCount,
    [property: JsonPropertyName("totalTokenCount")] int TotalTokenCount);
