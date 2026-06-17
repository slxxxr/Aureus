using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Workspaces.GetMyInvitations;

namespace Aureus.UnitTests.Workspaces;

public sealed class GetMyInvitationsHandlerTests
{
    [Fact]
    public async Task Handle_ValidQuery_ReturnsPendingInvitationsForUserEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "invitee@test.local", CreatedAt = DateTimeOffset.UtcNow };
        var summary = new PendingInvitationSummary(
            Guid.NewGuid(), Guid.NewGuid(), "Shared Workspace", DateTimeOffset.UtcNow.AddDays(7));

        var userRepo = new UserRepositoryMock().WithUserById(userId, user);
        var invitationRepo = new WorkspaceInvitationRepositoryMock()
            .WithPendingForEmail("invitee@test.local", [summary]);
        var handler = new GetMyInvitationsHandler(userRepo.Object, invitationRepo.Object);

        // Act
        var result = await handler.Handle(new GetMyInvitationsQuery(userId), CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(summary.Id, result[0].Id);
    }
}
