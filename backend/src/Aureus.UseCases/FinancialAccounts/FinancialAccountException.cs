using Aureus.UseCases.Common.Exceptions;

namespace Aureus.UseCases.FinancialAccounts;

public sealed class FinancialAccountException(FinancialAccountErrorCode code, string message)
    : UseCaseException(message)
{
    public FinancialAccountErrorCode Code { get; } = code;

    public override string ErrorCode => Code.ToString();

    public override ErrorType ErrorType => Code switch
    {
        FinancialAccountErrorCode.NameTaken => ErrorType.Conflict,
        FinancialAccountErrorCode.NotFound => ErrorType.NotFound,
        _ => ErrorType.Validation
    };
}
