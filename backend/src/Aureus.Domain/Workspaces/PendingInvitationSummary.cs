namespace Aureus.Domain.Workspaces;

public sealed record PendingInvitationSummary(Guid Id, Guid WorkspaceId, string WorkspaceName, DateTimeOffset ExpiresAt);
