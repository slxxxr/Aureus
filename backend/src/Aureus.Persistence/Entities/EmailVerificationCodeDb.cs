namespace Aureus.Persistence.Entities;

public sealed class EmailVerificationCodeDb
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Purpose { get; set; } = string.Empty;

    public string CodeHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public int AttemptsLeft { get; set; }

    public DateTimeOffset SentAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
