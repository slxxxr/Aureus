using Aureus.Postgres.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aureus.Postgres.Configurations;

public sealed class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMemberDb>
{
    private const int RoleMaxLength = 32;

    public void Configure(EntityTypeBuilder<WorkspaceMemberDb> builder)
    {
        builder.ToTable("workspace_members");

        builder.HasKey(member => member.Id);

        builder.Property(member => member.Id).HasColumnName("id");
        builder.Property(member => member.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(member => member.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(member => member.Role).HasColumnName("role").HasMaxLength(RoleMaxLength).IsRequired();
        builder.Property(member => member.JoinedAt).HasColumnName("joined_at").IsRequired();
        builder.Property(member => member.IsDeleted).HasColumnName("is_deleted").IsRequired();
        builder.Property(member => member.DeletedAt).HasColumnName("deleted_at");

        builder.HasQueryFilter(member => !member.IsDeleted);

        builder.HasIndex(member => new { member.WorkspaceId, member.UserId })
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");

        builder.HasOne(member => member.Workspace)
            .WithMany()
            .HasForeignKey(member => member.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserDb>()
            .WithMany()
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
