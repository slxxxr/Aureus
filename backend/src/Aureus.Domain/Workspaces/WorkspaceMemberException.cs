using Aureus.Domain.Exceptions;

namespace Aureus.Domain.Workspaces;

public sealed class WorkspaceMemberException(WorkspaceMemberErrorCode code, string message) : DomainException(message)
{
    public WorkspaceMemberErrorCode Code { get; } = code;

    public override string ErrorCode => Code.ToString();

    public override ErrorType ErrorType => Code switch
    {
        WorkspaceMemberErrorCode.MemberNotFound => ErrorType.NotFound,
        WorkspaceMemberErrorCode.CannotRemoveOwner => ErrorType.Forbidden,
        WorkspaceMemberErrorCode.InsufficientRole => ErrorType.Forbidden,
        WorkspaceMemberErrorCode.CannotLeaveAsOwner => ErrorType.Conflict,
        _ => ErrorType.Validation,
    };
}
