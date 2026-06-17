namespace Aureus.Api.Contracts.Workspaces;

public sealed record MyInvitationResponse(Guid Id, Guid WorkspaceId, string WorkspaceName, DateTimeOffset ExpiresAt);
