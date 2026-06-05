using Aureus.Domain.Exceptions;

namespace Aureus.Domain.FinancialAccounts;

public sealed class FinancialAccountException(FinancialAccountErrorCode code, string message) : DomainException(message)
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
