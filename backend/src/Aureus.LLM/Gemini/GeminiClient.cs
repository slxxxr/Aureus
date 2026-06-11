using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aureus.LLM.Gemini;

internal sealed class GeminiClient(
    HttpClient httpClient,
    IOptions<GeminiOptions> options,
    ILogger<GeminiClient> logger) : ILlmClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string> AskAsync(string prompt, CancellationToken cancellationToken)
    {
        var opts = options.Value;
        var url = $"/v1beta/models/{opts.Model}:generateContent?key={opts.ApiKey}";
        var request = new GeminiRequest([new GeminiContent([new GeminiPart(prompt)])]);

        var response = await httpClient.PostAsJsonAsync(url, request, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(_jsonOptions, cancellationToken);

        if (result?.UsageMetadata is { } usage)
        {
            logger.LogInformation(
                "Gemini tokens — prompt: {PromptTokens}, output: {OutputTokens}, total: {TotalTokens}",
                usage.PromptTokenCount,
                usage.CandidatesTokenCount,
                usage.TotalTokenCount);
        }

        return result?.Candidates?.FirstOrDefault()?.Content.Parts.FirstOrDefault()?.Text
            ?? throw new InvalidOperationException("Gemini returned an empty response.");
    }
}
