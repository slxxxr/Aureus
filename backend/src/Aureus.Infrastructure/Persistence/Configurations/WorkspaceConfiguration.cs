using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aureus.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    private const int NameMaxLength = 120;

    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("workspaces");

        builder.HasKey(workspace => workspace.Id);

        builder.Property(workspace => workspace.Id).HasColumnName("id");
        builder.Property(workspace => workspace.OwnerUserId).HasColumnName("owner_user_id").IsRequired();
        builder.Property(workspace => workspace.Name).HasColumnName("name").HasMaxLength(NameMaxLength).IsRequired();
        builder.Property(workspace => workspace.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(workspace => workspace.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(workspace => workspace.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
