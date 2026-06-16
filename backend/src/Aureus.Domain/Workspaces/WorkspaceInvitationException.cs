using Aureus.Domain.Exceptions;

namespace Aureus.Domain.Workspaces;

public sealed class WorkspaceInvitationException(WorkspaceInvitationErrorCode code, string message) : DomainException(message)
{
    public WorkspaceInvitationErrorCode Code { get; } = code;

    public override string ErrorCode => Code.ToString();

    public override ErrorType ErrorType => Code switch
    {
        WorkspaceInvitationErrorCode.NotFound => ErrorType.NotFound,
        WorkspaceInvitationErrorCode.AlreadyMember => ErrorType.Conflict,
        WorkspaceInvitationErrorCode.WorkspaceFull => ErrorType.Conflict,
        WorkspaceInvitationErrorCode.DailyQuotaExceeded => ErrorType.TooManyRequests,
        WorkspaceInvitationErrorCode.Forbidden => ErrorType.Forbidden,
        _ => ErrorType.Validation,
    };
}
