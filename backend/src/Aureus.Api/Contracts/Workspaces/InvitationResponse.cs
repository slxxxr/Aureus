namespace Aureus.Api.Contracts.Workspaces;

public sealed record InvitationResponse(Guid Id, string Email, Guid InvitedByUserId, DateTimeOffset ExpiresAt);
