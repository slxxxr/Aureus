namespace Aureus.Domain.Workspaces;

public enum WorkspaceMemberErrorCode
{
    MemberNotFound = 1,
    CannotRemoveOwner = 2,
    InsufficientRole = 3,
    CannotLeaveAsOwner = 4,
}
