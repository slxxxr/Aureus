using Aureus.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aureus.Persistence.Configurations;

public sealed class EmailVerificationCodeConfiguration : IEntityTypeConfiguration<EmailVerificationCodeDb>
{
    private const int MaxEmailLength = 254;
    private const int MaxPurposeLength = 32;
    private const int MaxCodeHashLength = 64;  // SHA-256 hex

    public void Configure(EntityTypeBuilder<EmailVerificationCodeDb> builder)
    {
        builder.ToTable("email_verification_codes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(MaxEmailLength).IsRequired();
        builder.Property(x => x.Purpose).HasColumnName("purpose").HasMaxLength(MaxPurposeLength).IsRequired();
        builder.Property(x => x.CodeHash).HasColumnName("code_hash").HasMaxLength(MaxCodeHashLength).IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(x => x.AttemptsLeft).HasColumnName("attempts_left").IsRequired();
        builder.Property(x => x.SentAt).HasColumnName("sent_at").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(x => new { x.Email, x.Purpose }).IsUnique();
    }
}
