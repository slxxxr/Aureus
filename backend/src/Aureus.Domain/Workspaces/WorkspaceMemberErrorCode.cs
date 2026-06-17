namespace Aureus.Domain.Workspaces;

public enum WorkspaceMemberErrorCode
{
    MemberNotFound = 1,
    CannotRemoveOwner = 2,
    InsufficientRole = 3,
    CannotLeaveAsOwner = 4,
    CannotTargetSelf = 5,
    CannotChangeOwnerRole = 6,
}
