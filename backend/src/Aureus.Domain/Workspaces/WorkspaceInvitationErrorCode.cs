namespace Aureus.Domain.Workspaces;

public enum WorkspaceInvitationErrorCode
{
    NotFound = 1,
    AlreadyMember = 2,
    WorkspaceFull = 3,
    DailyQuotaExceeded = 4,
    Forbidden = 5,
}
