namespace Aureus.LLM;

public interface ILlmClient
{
    Task<string> AskAsync(string prompt, CancellationToken cancellationToken);
}
