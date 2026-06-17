namespace Aureus.Persistence.Entities;

public sealed class WorkspaceInvitationDb
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public string Email { get; set; } = string.Empty;

    public Guid InvitedByUserId { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
