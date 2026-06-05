using Aureus.Postgres.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aureus.Postgres.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<UserDb>
{
    private const int EmailMaxLength = 254;
    private const int PasswordHashMaxLength = 512;

    public void Configure(EntityTypeBuilder<UserDb> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id).HasColumnName("id");
        builder.Property(user => user.Email).HasColumnName("email").HasMaxLength(EmailMaxLength).IsRequired();
        builder.Property(user => user.PasswordHash).HasColumnName("password_hash").HasMaxLength(PasswordHashMaxLength).IsRequired();
        builder.Property(user => user.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(user => user.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(user => user.Email).IsUnique();
    }
}
