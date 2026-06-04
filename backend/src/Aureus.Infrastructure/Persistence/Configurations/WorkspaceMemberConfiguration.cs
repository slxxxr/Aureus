using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aureus.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
{
    private const int RoleMaxLength = 32;

    public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        builder.ToTable("workspace_members");

        builder.HasKey(member => member.Id);

        builder.Property(member => member.Id).HasColumnName("id");
        builder.Property(member => member.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(member => member.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(member => member.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(RoleMaxLength).IsRequired();
        builder.Property(member => member.JoinedAt).HasColumnName("joined_at").IsRequired();

        builder.HasIndex(member => new { member.WorkspaceId, member.UserId }).IsUnique();

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(member => member.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
