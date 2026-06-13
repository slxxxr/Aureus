using Aureus.IntegrationTests.Common;
using Aureus.Persistence.Entities;
using Aureus.Postgres.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Aureus.IntegrationTests.Auth;

[Collection(nameof(PostgresCollection))]
public sealed class EmailVerificationCodeRepositoryTests(PostgresFixture fixture)
{
    private const string Purpose = "Registration";

    [Fact]
    public async Task FindAsync_NoPendingCode_ReturnsNull()
    {
        // Arrange
        var email = UniqueEmail();

        // Act
        EmailVerificationCodeDb? result;
        await using (var db = fixture.CreateDbContext())
        {
            result = await new EmailVerificationCodeRepository(db)
                .FindAsync(email, Purpose, CancellationToken.None);
        }

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindAsync_ExistingCode_ReturnsCode()
    {
        // Arrange
        var email = UniqueEmail();
        var seeded = await SeedCodeAsync(email);

        // Act
        EmailVerificationCodeDb? result;
        await using (var db = fixture.CreateDbContext())
        {
            result = await new EmailVerificationCodeRepository(db)
                .FindAsync(email, Purpose, CancellationToken.None);
        }

        // Assert
        Assert.NotNull(result);
        Assert.Equal(seeded.Id, result!.Id);
        Assert.Equal(seeded.CodeHash, result.CodeHash);
        Assert.Equal(seeded.AttemptsLeft, result.AttemptsLeft);
    }

    [Fact]
    public async Task UpsertAsync_NoExistingCode_InsertsNewRecord()
    {
        // Arrange
        var email = UniqueEmail();
        var code = NewCode(email);

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            await new EmailVerificationCodeRepository(db)
                .UpsertAsync(code, CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var stored = await assertDb.EmailVerificationCodes
            .FirstOrDefaultAsync(x => x.Email == email && x.Purpose == Purpose);

        Assert.NotNull(stored);
        Assert.Equal(code.Id, stored!.Id);
        Assert.Equal(code.CodeHash, stored.CodeHash);
        Assert.Equal(code.AttemptsLeft, stored.AttemptsLeft);
    }

    [Fact]
    public async Task UpsertAsync_ExistingCode_UpdatesCodeHashAndRetainsId()
    {
        // Arrange
        var email = UniqueEmail();
        var original = await SeedCodeAsync(email, codeHash: "oldhash");
        var updated = NewCode(email, id: original.Id, codeHash: "newhash");

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            await new EmailVerificationCodeRepository(db)
                .UpsertAsync(updated, CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var stored = await assertDb.EmailVerificationCodes
            .FirstOrDefaultAsync(x => x.Email == email && x.Purpose == Purpose);

        Assert.NotNull(stored);
        Assert.Equal(original.Id, stored!.Id);
        Assert.Equal("newhash", stored.CodeHash);
    }

    [Fact]
    public async Task UpsertAsync_DifferentPurposeSameEmail_InsertsSeparateRecord()
    {
        // Arrange
        var email = UniqueEmail();
        await SeedCodeAsync(email, purpose: "Registration");
        var other = NewCode(email, purpose: "PasswordReset");

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            await new EmailVerificationCodeRepository(db)
                .UpsertAsync(other, CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var count = await assertDb.EmailVerificationCodes
            .CountAsync(x => x.Email == email);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task DecrementAttemptsAsync_ExistingCode_DecrementsAttemptsLeft()
    {
        // Arrange
        var email = UniqueEmail();
        await SeedCodeAsync(email, attemptsLeft: 5);

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            await new EmailVerificationCodeRepository(db)
                .DecrementAttemptsAsync(email, Purpose, CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var stored = await assertDb.EmailVerificationCodes
            .FirstAsync(x => x.Email == email && x.Purpose == Purpose);

        Assert.Equal(4, stored.AttemptsLeft);
    }

    [Fact]
    public async Task DeleteAsync_ExistingCode_RemovesRecord()
    {
        // Arrange
        var email = UniqueEmail();
        await SeedCodeAsync(email);

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            await new EmailVerificationCodeRepository(db)
                .DeleteAsync(email, Purpose, CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var exists = await assertDb.EmailVerificationCodes
            .AnyAsync(x => x.Email == email && x.Purpose == Purpose);

        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteAsync_OtherPurposeCode_IsNotDeleted()
    {
        // Arrange
        var email = UniqueEmail();
        await SeedCodeAsync(email, purpose: "Registration");
        await SeedCodeAsync(email, purpose: "PasswordReset");

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            await new EmailVerificationCodeRepository(db)
                .DeleteAsync(email, "Registration", CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var remaining = await assertDb.EmailVerificationCodes
            .Where(x => x.Email == email)
            .ToListAsync();

        Assert.Single(remaining);
        Assert.Equal("PasswordReset", remaining[0].Purpose);
    }

    private static string UniqueEmail() => $"{Guid.NewGuid():N}@test.local";

    private static EmailVerificationCodeDb NewCode(
        string email,
        Guid? id = null,
        string? codeHash = null,
        string purpose = Purpose,
        int attemptsLeft = 10)
    {
        var now = DateTimeOffset.UtcNow;
        return new EmailVerificationCodeDb
        {
            Id = id ?? Guid.NewGuid(),
            Email = email,
            Purpose = purpose,
            CodeHash = codeHash ?? "testhash",
            ExpiresAt = now.AddHours(1),
            AttemptsLeft = attemptsLeft,
            SentAt = now,
            CreatedAt = now,
        };
    }

    private async Task<EmailVerificationCodeDb> SeedCodeAsync(
        string email,
        string purpose = Purpose,
        string codeHash = "testhash",
        int attemptsLeft = 10)
    {
        var code = NewCode(email, purpose: purpose, codeHash: codeHash, attemptsLeft: attemptsLeft);
        await using var db = fixture.CreateDbContext();
        await new EmailVerificationCodeRepository(db).UpsertAsync(code, CancellationToken.None);
        return code;
    }
}
