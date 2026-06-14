using Aureus.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aureus.Persistence.Configurations;

public sealed class WorkspaceDailyUsageConfiguration : IEntityTypeConfiguration<WorkspaceDailyUsageDb>
{
    private const int MaxFeatureLength = 32;

    public void Configure(EntityTypeBuilder<WorkspaceDailyUsageDb> builder)
    {
        builder.ToTable("workspace_daily_usage");

        builder.HasKey(x => new { x.WorkspaceId, x.Feature });

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.Feature)
            .HasColumnName("feature")
            .HasMaxLength(MaxFeatureLength)
            .HasConversion<string>()
            .IsRequired();
        builder.Property(x => x.LastDate).HasColumnName("last_date").IsRequired();
        builder.Property(x => x.Count).HasColumnName("count").IsRequired();
    }
}
