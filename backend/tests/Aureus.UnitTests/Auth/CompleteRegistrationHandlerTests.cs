using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Auth.Register.Complete;

namespace Aureus.UnitTests.Auth;

public sealed class CompleteRegistrationHandlerTests
{
    private const string Purpose = "Registration";

    private static CompleteRegistrationHandler BuildHandler(
        RegistrationTokenServiceMock tokenService,
        UserRepositoryMock userRepository,
        PasswordHasherMock passwordHasher,
        JwtTokenGeneratorMock jwtGenerator,
        WorkspaceInvitationRepositoryMock? invitationRepository = null,
        WorkspaceRepositoryMock? workspaceRepository = null) =>
        new(tokenService.Object,
            userRepository.Object,
            passwordHasher.Object,
            jwtGenerator.Object,
            (invitationRepository ?? new WorkspaceInvitationRepositoryMock()
                .WithPendingForEmail("user@example.com", [])).Object,
            (workspaceRepository ?? new WorkspaceRepositoryMock()).Object);

    [Fact]
    public async Task Handle_ValidToken_RegistersUserAndReturnsAccessToken()
    {
        // Arrange
        const string email = "user@example.com";
        const string token = "reg.token.value";
        const string accessToken = "jwt.access.token";

        var tokenService = new RegistrationTokenServiceMock().WithValidToken(token, email, Purpose);
        var userRepository = new UserRepositoryMock().CapturingRegistration();
        var passwordHasher = new PasswordHasherMock().WithHash("securepass", "hashed:securepass");
        var jwtGenerator = new JwtTokenGeneratorMock().WithAnyToken(accessToken);
        var invitationRepository = new WorkspaceInvitationRepositoryMock().WithPendingForEmail(email, []);
        var handler = BuildHandler(tokenService, userRepository, passwordHasher, jwtGenerator, invitationRepository);

        // Act
        var result = await handler.Handle(
            new CompleteRegistrationCommand(token, "Test User", "securepass"), CancellationToken.None);

        // Assert
        Assert.Equal(accessToken, result.AccessToken);
        userRepository.VerifyRegistrationSavedOnce();
        Assert.Equal(email, userRepository.SavedUser?.Email);
        Assert.Equal("hashed:securepass", userRepository.SavedUser?.PasswordHash);
        Assert.Equal("Personal", userRepository.SavedWorkspace?.Name);
    }

    [Fact]
    public async Task Handle_PendingInvitationExists_AcceptsIt()
    {
        // Arrange
        const string email = "user@example.com";
        const string token = "reg.token.value";

        var invitationWorkspaceId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();

        var tokenService = new RegistrationTokenServiceMock().WithValidToken(token, email, Purpose);
        var userRepository = new UserRepositoryMock().CapturingRegistration();
        var passwordHasher = new PasswordHasherMock().WithHash("securepass", "hashed:securepass");
        var jwtGenerator = new JwtTokenGeneratorMock().WithAnyToken("jwt.access.token");
        var invitationRepository = new WorkspaceInvitationRepositoryMock()
            .WithPendingForEmail(email, [new PendingInvitationSummary(invitationId, invitationWorkspaceId, "Shared", DateTimeOffset.UtcNow.AddDays(7))])
            .CapturingAccept();
        var workspaceRepository = new WorkspaceRepositoryMock().WithActiveMemberCount(invitationWorkspaceId, 1);
        var handler = BuildHandler(
            tokenService, userRepository, passwordHasher, jwtGenerator, invitationRepository, workspaceRepository);

        // Act
        await handler.Handle(new CompleteRegistrationCommand(token, "Test User", "securepass"), CancellationToken.None);

        // Assert
        Assert.Equal(invitationId, invitationRepository.AcceptedInvitationId);
        Assert.Equal(WorkspaceRole.Member, invitationRepository.AcceptedMember?.Role);
    }

    [Fact]
    public async Task Handle_PendingInvitationWorkspaceFull_SkipsIt()
    {
        // Arrange
        const string email = "user@example.com";
        const string token = "reg.token.value";

        var invitationWorkspaceId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();

        var tokenService = new RegistrationTokenServiceMock().WithValidToken(token, email, Purpose);
        var userRepository = new UserRepositoryMock().CapturingRegistration();
        var passwordHasher = new PasswordHasherMock().WithHash("securepass", "hashed:securepass");
        var jwtGenerator = new JwtTokenGeneratorMock().WithAnyToken("jwt.access.token");
        var invitationRepository = new WorkspaceInvitationRepositoryMock()
            .WithPendingForEmail(email, [new PendingInvitationSummary(invitationId, invitationWorkspaceId, "Shared", DateTimeOffset.UtcNow.AddDays(7))])
            .CapturingAccept();
        var workspaceRepository = new WorkspaceRepositoryMock()
            .WithActiveMemberCount(invitationWorkspaceId, WorkspaceLimits.MaxMembers);
        var handler = BuildHandler(
            tokenService, userRepository, passwordHasher, jwtGenerator, invitationRepository, workspaceRepository);

        // Act
        await handler.Handle(new CompleteRegistrationCommand(token, "Test User", "securepass"), CancellationToken.None);

        // Assert
        invitationRepository.VerifyAcceptNotCalled();
    }

    [Fact]
    public async Task Handle_InvalidToken_ThrowsEmailVerificationException()
    {
        // Arrange
        const string token = "bad.token";

        var tokenService = new RegistrationTokenServiceMock().WithInvalidToken(token);
        var userRepository = new UserRepositoryMock();
        var passwordHasher = new PasswordHasherMock();
        var jwtGenerator = new JwtTokenGeneratorMock();
        var handler = BuildHandler(tokenService, userRepository, passwordHasher, jwtGenerator);

        // Act
        var exception = await Assert.ThrowsAsync<EmailVerificationException>(() =>
            handler.Handle(new CompleteRegistrationCommand(token, "Test User", "securepass"), CancellationToken.None));

        // Assert
        Assert.Equal(EmailVerificationErrorCode.RegistrationTokenInvalid, exception.Code);
        userRepository.VerifyRegistrationNotSaved();
    }

    [Fact]
    public async Task Handle_PasswordTooShort_ThrowsEmailVerificationException()
    {
        // Arrange
        const string token = "reg.token.value";
        const string email = "user@example.com";

        var tokenService = new RegistrationTokenServiceMock().WithValidToken(token, email, Purpose);
        var userRepository = new UserRepositoryMock();
        var passwordHasher = new PasswordHasherMock();
        var jwtGenerator = new JwtTokenGeneratorMock();
        var handler = BuildHandler(tokenService, userRepository, passwordHasher, jwtGenerator);

        // Act
        var exception = await Assert.ThrowsAsync<EmailVerificationException>(() =>
            handler.Handle(new CompleteRegistrationCommand(token, "Test User", "short"), CancellationToken.None));

        // Assert
        Assert.Equal(EmailVerificationErrorCode.InvalidPassword, exception.Code);
        userRepository.VerifyRegistrationNotSaved();
    }
}
