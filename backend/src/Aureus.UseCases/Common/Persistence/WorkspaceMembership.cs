using Aureus.Domain.Workspaces;

namespace Aureus.UseCases.Common.Persistence;

public sealed record WorkspaceMembership(Guid WorkspaceId, Guid UserId, WorkspaceRole Role);
