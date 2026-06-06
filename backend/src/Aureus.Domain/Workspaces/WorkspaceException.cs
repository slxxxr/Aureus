using Aureus.Domain.Exceptions;

namespace Aureus.Domain.Workspaces;

public sealed class WorkspaceException(WorkspaceErrorCode code, string message) : DomainException(message)
{
    public WorkspaceErrorCode Code { get; } = code;

    public override string ErrorCode => Code.ToString();

    public override ErrorType ErrorType => Code switch
    {
        WorkspaceErrorCode.NameTaken => ErrorType.Conflict,
        WorkspaceErrorCode.NotFound => ErrorType.NotFound,
        WorkspaceErrorCode.Forbidden => ErrorType.Unauthorized,
        _ => ErrorType.Validation,
    };
}
