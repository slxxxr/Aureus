using Aureus.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aureus.Persistence.Configurations;

public sealed class TransferConfiguration : IEntityTypeConfiguration<TransferDb>
{
    private const int CurrencyCodeMaxLength = 3;
    private const int NoteMaxLength = 500;

    public void Configure(EntityTypeBuilder<TransferDb> builder)
    {
        builder.ToTable("transfers");

        builder.HasKey(transfer => transfer.Id);

        builder.Property(transfer => transfer.Id).HasColumnName("id");
        builder.Property(transfer => transfer.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(transfer => transfer.FromAccountId).HasColumnName("from_account_id").IsRequired();
        builder.Property(transfer => transfer.ToAccountId).HasColumnName("to_account_id").IsRequired();
        builder.Property(transfer => transfer.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(transfer => transfer.AmountMinor).HasColumnName("amount_minor").IsRequired();
        builder.Property(transfer => transfer.Currency).HasColumnName("currency").HasMaxLength(CurrencyCodeMaxLength).IsRequired();
        builder.Property(transfer => transfer.OccurredAt).HasColumnName("occurred_at").HasColumnType("date").IsRequired();
        builder.Property(transfer => transfer.Note).HasColumnName("note").HasMaxLength(NoteMaxLength);
        builder.Property(transfer => transfer.IsDeleted).HasColumnName("is_deleted").IsRequired();
        builder.Property(transfer => transfer.DeletedAt).HasColumnName("deleted_at");
        builder.Property(transfer => transfer.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(transfer => transfer.UpdatedAt).HasColumnName("updated_at");

        builder.HasQueryFilter(transfer => !transfer.IsDeleted);

        builder.HasIndex(transfer => new { transfer.WorkspaceId, transfer.OccurredAt });
        builder.HasIndex(transfer => new { transfer.FromAccountId, transfer.IsDeleted });
        builder.HasIndex(transfer => new { transfer.ToAccountId, transfer.IsDeleted });
        builder.HasIndex(transfer => transfer.CreatedByUserId);

        builder.HasOne<WorkspaceDb>()
            .WithMany()
            .HasForeignKey(transfer => transfer.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FinancialAccountDb>()
            .WithMany()
            .HasForeignKey(transfer => transfer.FromAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FinancialAccountDb>()
            .WithMany()
            .HasForeignKey(transfer => transfer.ToAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserDb>()
            .WithMany()
            .HasForeignKey(transfer => transfer.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
