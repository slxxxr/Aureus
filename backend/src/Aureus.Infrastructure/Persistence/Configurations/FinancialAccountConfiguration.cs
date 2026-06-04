using Aureus.Domain.FinancialAccounts;
using Aureus.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aureus.Infrastructure.Persistence.Configurations;

public sealed class FinancialAccountConfiguration : IEntityTypeConfiguration<FinancialAccount>
{
    private const int NameMaxLength = 120;
    private const int CurrencyCodeMaxLength = 3;

    public void Configure(EntityTypeBuilder<FinancialAccount> builder)
    {
        builder.ToTable("financial_accounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id).HasColumnName("id");
        builder.Property(account => account.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(account => account.Name).HasColumnName("name").HasMaxLength(NameMaxLength).IsRequired();
        builder.Property(account => account.Currency).HasColumnName("currency").HasMaxLength(CurrencyCodeMaxLength).IsRequired();
        builder.Property(account => account.InitialBalanceMinor).HasColumnName("initial_balance_minor").IsRequired();
        builder.Property(account => account.CurrentBalanceMinor).HasColumnName("current_balance_minor").IsRequired();
        builder.Property(account => account.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(account => account.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(account => new { account.WorkspaceId, account.Name }).IsUnique();

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(account => account.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
