using Aureus.Postgres.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aureus.Postgres.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<CategoryDb>
{
    private const int NameMaxLength = 120;
    private const int TypeMaxLength = 32;

    public void Configure(EntityTypeBuilder<CategoryDb> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Id).HasColumnName("id");
        builder.Property(category => category.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(category => category.Name).HasColumnName("name").HasMaxLength(NameMaxLength).IsRequired();
        builder.Property(category => category.Type).HasColumnName("type").HasMaxLength(TypeMaxLength).IsRequired();
        builder.Property(category => category.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(category => category.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(category => new { category.WorkspaceId, category.Name, category.Type }).IsUnique();

        builder.HasOne<WorkspaceDb>()
            .WithMany()
            .HasForeignKey(category => category.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
