using Aureus.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aureus.Persistence.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<TransactionDb>
{
    private const int NameMaxLength = 200;
    private const int TypeMaxLength = 32;
    private const int CurrencyCodeMaxLength = 3;
    private const int NoteMaxLength = 500;

    public void Configure(EntityTypeBuilder<TransactionDb> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Id).HasColumnName("id");
        builder.Property(transaction => transaction.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(transaction => transaction.FinancialAccountId).HasColumnName("financial_account_id").IsRequired();
        builder.Property(transaction => transaction.CategoryId).HasColumnName("category_id").IsRequired();
        builder.Property(transaction => transaction.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(transaction => transaction.Name).HasColumnName("name").HasMaxLength(NameMaxLength).IsRequired();
        builder.Property(transaction => transaction.Type).HasColumnName("type").HasMaxLength(TypeMaxLength).IsRequired();
        builder.Property(transaction => transaction.AmountMinor).HasColumnName("amount_minor").IsRequired();
        builder.Property(transaction => transaction.Currency).HasColumnName("currency").HasMaxLength(CurrencyCodeMaxLength).IsRequired();
        builder.Property(transaction => transaction.OccurredAt).HasColumnName("occurred_at").HasColumnType("date").IsRequired();
        builder.Property(transaction => transaction.Note).HasColumnName("note").HasMaxLength(NoteMaxLength);
        builder.Property(transaction => transaction.IsDeleted).HasColumnName("is_deleted").IsRequired();
        builder.Property(transaction => transaction.DeletedAt).HasColumnName("deleted_at");
        builder.Property(transaction => transaction.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(transaction => transaction.UpdatedAt).HasColumnName("updated_at");

        builder.HasQueryFilter(transaction => !transaction.IsDeleted);

        builder.HasIndex(transaction => new { transaction.WorkspaceId, transaction.OccurredAt });
        builder.HasIndex(transaction => new { transaction.FinancialAccountId, transaction.IsDeleted });
        builder.HasIndex(transaction => transaction.CreatedByUserId);

        builder.HasOne<WorkspaceDb>()
            .WithMany()
            .HasForeignKey(transaction => transaction.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FinancialAccountDb>()
            .WithMany()
            .HasForeignKey(transaction => transaction.FinancialAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CategoryDb>()
            .WithMany()
            .HasForeignKey(transaction => transaction.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserDb>()
            .WithMany()
            .HasForeignKey(transaction => transaction.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
