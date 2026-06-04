using Aureus.Domain.Categories;
using Aureus.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aureus.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    private const int NameMaxLength = 120;
    private const int TypeMaxLength = 32;

    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Id).HasColumnName("id");
        builder.Property(category => category.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(category => category.Name).HasColumnName("name").HasMaxLength(NameMaxLength).IsRequired();
        builder.Property(category => category.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(TypeMaxLength).IsRequired();
        builder.Property(category => category.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(category => category.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(category => new { category.WorkspaceId, category.Name, category.Type }).IsUnique();

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(category => category.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
