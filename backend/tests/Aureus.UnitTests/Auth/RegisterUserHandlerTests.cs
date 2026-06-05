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
        var userRepository = new UserRepositoryMock()
            .WithAvailableEmail("user@example.com")
            .CapturingRegistration();
        var passwordHasher = new PasswordHasherMock()
            .WithHash("password123", "hashed:password123");
        var handler = new RegisterUserHandler(userRepository.Object, passwordHasher.Object);

        // Act
        var result = await handler.Handle(
            new RegisterUserCommand(" User@Example.com ", "password123"),
            CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.UserId);
        Assert.NotEqual(Guid.Empty, result.WorkspaceId);
        Assert.NotNull(userRepository.SavedUser);
        Assert.NotNull(userRepository.SavedWorkspace);
        Assert.NotNull(userRepository.SavedWorkspaceMember);
        Assert.Equal("user@example.com", userRepository.SavedUser.Email);
        Assert.Equal("hashed:password123", userRepository.SavedUser.PasswordHash);
        Assert.Equal(result.UserId, userRepository.SavedUser.Id);
        Assert.Equal(result.WorkspaceId, userRepository.SavedWorkspace.Id);
        Assert.Equal(result.UserId, userRepository.SavedWorkspace.OwnerUserId);
        Assert.Equal(result.WorkspaceId, userRepository.SavedWorkspaceMember.WorkspaceId);
        Assert.Equal(result.UserId, userRepository.SavedWorkspaceMember.UserId);
        Assert.Equal(WorkspaceRole.Owner, userRepository.SavedWorkspaceMember.Role);
        userRepository.VerifyRegistrationSavedOnce();
    }

    [Theory]
    [InlineData("юзер@example.com")]
    [InlineData("user@пример.рф")]
    [InlineData("user@example.рф")]
    public async Task Handle_NonAsciiEmail_ThrowsRegistrationException(string email)
    {
        // Arrange
        var userRepository = new UserRepositoryMock();
        var passwordHasher = new PasswordHasherMock();
        var handler = new RegisterUserHandler(userRepository.Object, passwordHasher.Object);

        // Act
        var exception = await Assert.ThrowsAsync<RegistrationException>(() =>
            handler.Handle(
                new RegisterUserCommand(email, "password123"),
                CancellationToken.None));

        // Assert
        Assert.Equal(RegistrationErrorCode.InvalidEmail, exception.Code);
        userRepository.VerifyRegistrationNotSaved();
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_ThrowsRegistrationException()
    {
        // Arrange
        var userRepository = new UserRepositoryMock()
            .WithExistingEmail("user@example.com");
        var passwordHasher = new PasswordHasherMock();
        var handler = new RegisterUserHandler(userRepository.Object, passwordHasher.Object);

        // Act
        var exception = await Assert.ThrowsAsync<RegistrationException>(() =>
            handler.Handle(
                new RegisterUserCommand("user@example.com", "password123"),
                CancellationToken.None));

        // Assert
        Assert.Equal(RegistrationErrorCode.EmailAlreadyExists, exception.Code);
        userRepository.VerifyRegistrationNotSaved();
    }
}
