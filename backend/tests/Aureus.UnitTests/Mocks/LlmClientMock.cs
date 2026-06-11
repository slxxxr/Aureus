using Aureus.LLM;
using Moq;

namespace Aureus.UnitTests.Mocks;

public sealed class LlmClientMock
{
    private readonly Mock<ILlmClient> _mock = new();

    public ILlmClient Object => _mock.Object;

    public string? CapturedPrompt { get; private set; }

    public LlmClientMock WithAnswer(string answer)
    {
        _mock.Setup(c => c.AskAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(answer);
        return this;
    }

    public LlmClientMock CapturingPrompt()
    {
        _mock.Setup(c => c.AskAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((prompt, _) => CapturedPrompt = prompt)
            .ReturnsAsync("answer");
        return this;
    }
}
