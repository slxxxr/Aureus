using Aureus.Domain.Workspaces;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Workspaces.GetWorkspaceInvitations;

namespace Aureus.UnitTests.Workspaces;

public sealed class GetWorkspaceInvitationsHandlerTests
{
    [Fact]
    public async Task Handle_ValidQuery_ReturnsPendingInvitations()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var invitation = new WorkspaceInvitation
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Email = "invitee@test.local",
            InvitedByUserId = Guid.NewGuid(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var repo = new WorkspaceInvitationRepositoryMock().WithPendingForWorkspace(workspaceId, [invitation]);
        var handler = new GetWorkspaceInvitationsHandler(repo.Object);

        // Act
        var result = await handler.Handle(new GetWorkspaceInvitationsQuery(workspaceId), CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(invitation.Id, result[0].Id);
    }
}
