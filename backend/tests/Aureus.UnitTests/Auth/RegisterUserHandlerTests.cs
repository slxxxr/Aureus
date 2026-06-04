using Aureus.Domain.Workspaces;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Auth.Register;

namespace Aureus.UnitTests.Auth;

public sealed class RegisterUserHandlerTests
{
    [Fact]
    public async Task Handle_UserDoesNotExist_CreatesUserWorkspaceAndOwnerMembership()
    {
        // Arrange
        var registrationDb = new UserRegistrationDbMock()
            .WithAvailableEmail("user@example.com")
            .CapturingRegistration();
        var passwordHasher = new PasswordHasherMock()
            .WithHash("password123", "hashed:password123");
        var handler = new RegisterUserHandler(registrationDb.Object, passwordHasher.Object);

        // Act
        var result = await handler.Handle(
            new RegisterUserCommand(" User@Example.com ", "password123"),
            CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.UserId);
        Assert.NotEqual(Guid.Empty, result.WorkspaceId);
        Assert.NotNull(registrationDb.SavedUser);
        Assert.NotNull(registrationDb.SavedWorkspace);
        Assert.NotNull(registrationDb.SavedWorkspaceMember);
        Assert.Equal("user@example.com", registrationDb.SavedUser.Email);
        Assert.Equal("hashed:password123", registrationDb.SavedUser.PasswordHash);
        Assert.Equal(result.UserId, registrationDb.SavedUser.Id);
        Assert.Equal(result.WorkspaceId, registrationDb.SavedWorkspace.Id);
        Assert.Equal(result.UserId, registrationDb.SavedWorkspace.OwnerUserId);
        Assert.Equal(result.WorkspaceId, registrationDb.SavedWorkspaceMember.WorkspaceId);
        Assert.Equal(result.UserId, registrationDb.SavedWorkspaceMember.UserId);
        Assert.Equal(WorkspaceRole.Owner, registrationDb.SavedWorkspaceMember.Role);
        registrationDb.VerifyRegistrationSavedOnce();
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_ThrowsRegistrationException()
    {
        // Arrange
        var registrationDb = new UserRegistrationDbMock()
            .WithExistingEmail("user@example.com");
        var passwordHasher = new PasswordHasherMock();
        var handler = new RegisterUserHandler(registrationDb.Object, passwordHasher.Object);

        // Act
        var exception = await Assert.ThrowsAsync<RegistrationException>(() =>
            handler.Handle(
                new RegisterUserCommand("user@example.com", "password123"),
                CancellationToken.None));

        // Assert
        Assert.Equal(RegistrationErrorCode.EmailAlreadyExists, exception.Code);
        registrationDb.VerifyRegistrationNotSaved();
    }
}
