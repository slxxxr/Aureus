using System.Text.Json.Serialization;

namespace Aureus.LLM.Gemini;

internal sealed record GeminiRequest(
    [property: JsonPropertyName("contents")] List<GeminiContent> Contents);

internal sealed record GeminiContent(
    [property: JsonPropertyName("parts")] List<GeminiPart> Parts);

internal sealed record GeminiPart(
    [property: JsonPropertyName("text")] string Text);
