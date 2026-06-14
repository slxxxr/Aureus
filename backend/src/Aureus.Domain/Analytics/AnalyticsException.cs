using Aureus.Domain.Exceptions;

namespace Aureus.Domain.Analytics;

public sealed class AnalyticsException(AnalyticsErrorCode code, string message) : DomainException(message)
{
    public AnalyticsErrorCode Code { get; } = code;

    public override string ErrorCode => Code.ToString();

    public override ErrorType ErrorType => Code switch
    {
        AnalyticsErrorCode.DailyQuotaExceeded => ErrorType.TooManyRequests,
        AnalyticsErrorCode.LlmTemporarilyUnavailable => ErrorType.TooManyRequests,
        _ => ErrorType.Validation
    };
}
