using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
using Moq;

namespace Aureus.UnitTests.Mocks;

public sealed class WorkspaceInvitationRepositoryMock
{
    private readonly Mock<IWorkspaceInvitationRepository> _mock = new();

    public IWorkspaceInvitationRepository Object => _mock.Object;

    public WorkspaceInvitation? UpsertedInvitation { get; private set; }

    public Guid? DeletedInvitationId { get; private set; }

    public Guid? AcceptedInvitationId { get; private set; }

    public WorkspaceMember? AcceptedMember { get; private set; }

    public WorkspaceInvitationRepositoryMock WithInvitation(Guid id, WorkspaceInvitation invitation)
    {
        _mock.Setup(r => r.FindByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(invitation);
        return this;
    }

    public WorkspaceInvitationRepositoryMock WithNoInvitation(Guid id)
    {
        _mock.Setup(r => r.FindByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((WorkspaceInvitation?)null);
        return this;
    }

    public WorkspaceInvitationRepositoryMock WithPendingInvitation(Guid workspaceId, string email, WorkspaceInvitation? invitation)
    {
        _mock.Setup(r => r.FindPendingAsync(workspaceId, email, It.IsAny<CancellationToken>())).ReturnsAsync(invitation);
        return this;
    }

    public WorkspaceInvitationRepositoryMock WithActiveCount(Guid workspaceId, int count)
    {
        _mock
            .Setup(r => r.CountActiveAsync(workspaceId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(count);

        return this;
    }

    public WorkspaceInvitationRepositoryMock WithPendingForEmail(string email, IReadOnlyList<PendingInvitationSummary> summaries)
    {
        _mock
            .Setup(r => r.GetPendingForEmailAsync(email, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaries);

        return this;
    }

    public WorkspaceInvitationRepositoryMock WithPendingForWorkspace(Guid workspaceId, IReadOnlyList<WorkspaceInvitation> invitations)
    {
        _mock
            .Setup(r => r.GetPendingForWorkspaceAsync(workspaceId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitations);

        return this;
    }

    public WorkspaceInvitationRepositoryMock CapturingUpsert()
    {
        _mock
            .Setup(r => r.UpsertAsync(It.IsAny<WorkspaceInvitation>(), It.IsAny<CancellationToken>()))
            .Callback<WorkspaceInvitation, CancellationToken>((invitation, _) => UpsertedInvitation = invitation)
            .Returns(Task.CompletedTask);

        return this;
    }

    public WorkspaceInvitationRepositoryMock CapturingDelete()
    {
        _mock
            .Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((id, _) => DeletedInvitationId = id)
            .Returns(Task.CompletedTask);

        return this;
    }

    public WorkspaceInvitationRepositoryMock CapturingAccept()
    {
        _mock
            .Setup(r => r.AcceptAsync(It.IsAny<Guid>(), It.IsAny<WorkspaceMember>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, WorkspaceMember, CancellationToken>((id, member, _) =>
            {
                AcceptedInvitationId = id;
                AcceptedMember = member;
            })
            .Returns(Task.CompletedTask);

        return this;
    }

    public void VerifyDeleteNotCalled() =>
        _mock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

    public void VerifyAcceptNotCalled() =>
        _mock.Verify(r => r.AcceptAsync(It.IsAny<Guid>(), It.IsAny<WorkspaceMember>(), It.IsAny<CancellationToken>()), Times.Never);
}
