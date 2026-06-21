using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Auth.Register.Complete;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aureus.UnitTests.Auth;

public sealed class CompleteRegistrationHandlerTests
{
    private const string Purpose = "Registration";

    private static CompleteRegistrationHandler BuildHandler(
        RegistrationTokenServiceMock tokenService,
        RegistrationRepositoryMock registrationRepository,
        PasswordHasherMock passwordHasher,
        JwtTokenGeneratorMock jwtGenerator,
        WorkspaceInvitationRepositoryMock? invitationRepository = null,
        WorkspaceRepositoryMock? workspaceRepository = null,
        FinancialAccountRepositoryMock? accountRepository = null,
        CategoryRepositoryMock? categoryRepository = null) =>
        new(tokenService.Object,
            registrationRepository.Object,
            passwordHasher.Object,
            jwtGenerator.Object,
            (invitationRepository ?? new WorkspaceInvitationRepositoryMock()
                .WithPendingForEmail("user@example.com", [])).Object,
            (workspaceRepository ?? new WorkspaceRepositoryMock()).Object,
            (accountRepository ?? new FinancialAccountRepositoryMock()).Object,
            (categoryRepository ?? new CategoryRepositoryMock()).Object,
            NullLogger<CompleteRegistrationHandler>.Instance);

    [Fact]
    public async Task Handle_ValidToken_RegistersUserAndReturnsAccessToken()
    {
        // Arrange
        const string email = "user@example.com";
        const string token = "reg.token.value";
        const string accessToken = "jwt.access.token";

        var tokenService = new RegistrationTokenServiceMock().WithValidToken(token, email, Purpose);
        var registrationRepository = new RegistrationRepositoryMock().CapturingCreate();
        var passwordHasher = new PasswordHasherMock().WithHash("securepass", "hashed:securepass");
        var jwtGenerator = new JwtTokenGeneratorMock().WithAnyToken(accessToken);
        var invitationRepository = new WorkspaceInvitationRepositoryMock().WithPendingForEmail(email, []);
        var handler = BuildHandler(tokenService, registrationRepository, passwordHasher, jwtGenerator, invitationRepository);

        // Act
        var result = await handler.Handle(
            new CompleteRegistrationCommand(token, "Test User", "securepass", null), CancellationToken.None);

        // Assert
        Assert.Equal(accessToken, result.AccessToken);
        registrationRepository.VerifyCreateCalledOnce();
        Assert.Equal(email, registrationRepository.SavedUser?.Email);
        Assert.Equal("hashed:securepass", registrationRepository.SavedUser?.PasswordHash);
        Assert.Equal("Личное", registrationRepository.SavedWorkspace?.Name);
        Assert.Equal(WorkspaceRole.Owner, registrationRepository.SavedMember?.Role);
        Assert.Equal(registrationRepository.SavedWorkspace?.Id, registrationRepository.SavedMember?.WorkspaceId);
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
        var registrationRepository = new RegistrationRepositoryMock().CapturingCreate();
        var passwordHasher = new PasswordHasherMock().WithHash("securepass", "hashed:securepass");
        var jwtGenerator = new JwtTokenGeneratorMock().WithAnyToken("jwt.access.token");
        var invitationRepository = new WorkspaceInvitationRepositoryMock()
            .WithPendingForEmail(email, [new PendingInvitationSummary(invitationId, invitationWorkspaceId, "Shared", DateTimeOffset.UtcNow.AddDays(7))])
            .CapturingAccept();
        var workspaceRepository = new WorkspaceRepositoryMock().WithActiveMemberCount(invitationWorkspaceId, 1);
        var handler = BuildHandler(
            tokenService, registrationRepository, passwordHasher, jwtGenerator, invitationRepository, workspaceRepository);

        // Act
        await handler.Handle(new CompleteRegistrationCommand(token, "Test User", "securepass", null), CancellationToken.None);

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
        var registrationRepository = new RegistrationRepositoryMock().CapturingCreate();
        var passwordHasher = new PasswordHasherMock().WithHash("securepass", "hashed:securepass");
        var jwtGenerator = new JwtTokenGeneratorMock().WithAnyToken("jwt.access.token");
        var invitationRepository = new WorkspaceInvitationRepositoryMock()
            .WithPendingForEmail(email, [new PendingInvitationSummary(invitationId, invitationWorkspaceId, "Shared", DateTimeOffset.UtcNow.AddDays(7))])
            .CapturingAccept();
        var workspaceRepository = new WorkspaceRepositoryMock()
            .WithActiveMemberCount(invitationWorkspaceId, WorkspaceLimits.MaxMembers);
        var handler = BuildHandler(
            tokenService, registrationRepository, passwordHasher, jwtGenerator, invitationRepository, workspaceRepository);

        // Act
        await handler.Handle(new CompleteRegistrationCommand(token, "Test User", "securepass", null), CancellationToken.None);

        // Assert
        invitationRepository.VerifyAcceptNotCalled();
    }

    [Fact]
    public async Task Handle_EnglishLanguage_SeedsEnglishWorkspace()
    {
        // Arrange
        const string email = "user@example.com";
        const string token = "reg.token.value";

        var tokenService = new RegistrationTokenServiceMock().WithValidToken(token, email, Purpose);
        var registrationRepository = new RegistrationRepositoryMock().CapturingCreate();
        var passwordHasher = new PasswordHasherMock().WithHash("securepass", "hashed:securepass");
        var jwtGenerator = new JwtTokenGeneratorMock().WithAnyToken("token");
        var invitationRepository = new WorkspaceInvitationRepositoryMock().WithPendingForEmail(email, []);
        var handler = BuildHandler(tokenService, registrationRepository, passwordHasher, jwtGenerator, invitationRepository);

        // Act
        await handler.Handle(
            new CompleteRegistrationCommand(token, "Test User", "securepass", "en"), CancellationToken.None);

        // Assert
        Assert.Equal("Personal", registrationRepository.SavedWorkspace?.Name);
    }

    [Fact]
    public async Task Handle_NormalizesWhitespace()
    {
        // Arrange
        const string email = "user@example.com";
        const string token = "reg.token.value";

        var tokenService = new RegistrationTokenServiceMock().WithValidToken(token, email, Purpose);
        var registrationRepository = new RegistrationRepositoryMock().CapturingCreate();
        var passwordHasher = new PasswordHasherMock().WithHash("securepass", "hashed:securepass");
        var jwtGenerator = new JwtTokenGeneratorMock().WithAnyToken("token");
        var invitationRepository = new WorkspaceInvitationRepositoryMock().WithPendingForEmail(email, []);
        var handler = BuildHandler(tokenService, registrationRepository, passwordHasher, jwtGenerator, invitationRepository);

        // Act
        await handler.Handle(
            new CompleteRegistrationCommand(token, "  Test User  ", "securepass", null), CancellationToken.None);

        // Assert
        Assert.Equal("Test User", registrationRepository.SavedUser?.Name);
    }

    [Fact]
    public async Task Handle_InvalidToken_ThrowsEmailVerificationException()
    {
        // Arrange
        const string token = "bad.token";

        var tokenService = new RegistrationTokenServiceMock().WithInvalidToken(token);
        var registrationRepository = new RegistrationRepositoryMock();
        var passwordHasher = new PasswordHasherMock();
        var jwtGenerator = new JwtTokenGeneratorMock();
        var handler = BuildHandler(tokenService, registrationRepository, passwordHasher, jwtGenerator);

        // Act
        var exception = await Assert.ThrowsAsync<EmailVerificationException>(() =>
            handler.Handle(new CompleteRegistrationCommand(token, "Test User", "securepass", null), CancellationToken.None));

        // Assert
        Assert.Equal(EmailVerificationErrorCode.RegistrationTokenInvalid, exception.Code);
        registrationRepository.VerifyCreateNotCalled();
    }

    [Fact]
    public async Task Handle_PasswordTooShort_ThrowsEmailVerificationException()
    {
        // Arrange
        const string token = "reg.token.value";
        const string email = "user@example.com";

        var tokenService = new RegistrationTokenServiceMock().WithValidToken(token, email, Purpose);
        var registrationRepository = new RegistrationRepositoryMock();
        var passwordHasher = new PasswordHasherMock();
        var jwtGenerator = new JwtTokenGeneratorMock();
        var handler = BuildHandler(tokenService, registrationRepository, passwordHasher, jwtGenerator);

        // Act
        var exception = await Assert.ThrowsAsync<EmailVerificationException>(() =>
            handler.Handle(new CompleteRegistrationCommand(token, "Test User", "short", null), CancellationToken.None));

        // Assert
        Assert.Equal(EmailVerificationErrorCode.InvalidPassword, exception.Code);
        registrationRepository.VerifyCreateNotCalled();
    }
}
