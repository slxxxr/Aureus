namespace Aureus.LLM.Gemini;

public sealed class GeminiOptions
{
    public const string Section = "Gemini";

    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = string.Empty;
}
