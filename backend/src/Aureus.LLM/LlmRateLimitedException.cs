namespace Aureus.LLM;

public sealed class LlmRateLimitedException(string message) : Exception(message);