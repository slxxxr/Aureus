using Aureus.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aureus.Persistence.Configurations;

public sealed class WorkspaceConfiguration : IEntityTypeConfiguration<WorkspaceDb>
{
    private const int NameMaxLength = 120;

    public void Configure(EntityTypeBuilder<WorkspaceDb> builder)
    {
        builder.ToTable("workspaces");

        builder.HasKey(workspace => workspace.Id);

        builder.Property(workspace => workspace.Id).HasColumnName("id");
        builder.Property(workspace => workspace.OwnerUserId).HasColumnName("owner_user_id").IsRequired();
        builder.Property(workspace => workspace.Name).HasColumnName("name").HasMaxLength(NameMaxLength).IsRequired();
        builder.Property(workspace => workspace.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(workspace => workspace.UpdatedAt).HasColumnName("updated_at");
        builder.Property(workspace => workspace.IsDeleted).HasColumnName("is_deleted").IsRequired();
        builder.Property(workspace => workspace.DeletedAt).HasColumnName("deleted_at");

        builder.HasQueryFilter(workspace => !workspace.IsDeleted);

        builder.HasOne<UserDb>()
            .WithMany()
            .HasForeignKey(workspace => workspace.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(workspace => new { workspace.OwnerUserId, workspace.Name })
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");
    }
}
