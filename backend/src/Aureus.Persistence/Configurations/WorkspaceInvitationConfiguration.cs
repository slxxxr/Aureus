using Aureus.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aureus.Persistence.Configurations;

public sealed class WorkspaceInvitationConfiguration : IEntityTypeConfiguration<WorkspaceInvitationDb>
{
    private const int MaxEmailLength = 254;

    public void Configure(EntityTypeBuilder<WorkspaceInvitationDb> builder)
    {
        builder.ToTable("workspace_invitations");

        builder.HasKey(invitation => invitation.Id);

        builder.Property(invitation => invitation.Id).HasColumnName("id");
        builder.Property(invitation => invitation.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(invitation => invitation.Email).HasColumnName("email").HasMaxLength(MaxEmailLength).IsRequired();
        builder.Property(invitation => invitation.InvitedByUserId).HasColumnName("invited_by_user_id").IsRequired();
        builder.Property(invitation => invitation.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(invitation => invitation.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(invitation => new { invitation.WorkspaceId, invitation.Email }).IsUnique();
        builder.HasIndex(invitation => invitation.Email);

        builder.HasOne<WorkspaceDb>()
            .WithMany()
            .HasForeignKey(invitation => invitation.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserDb>()
            .WithMany()
            .HasForeignKey(invitation => invitation.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
