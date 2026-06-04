using Aureus.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aureus.Infrastructure;

public sealed class AureusDbContext(DbContextOptions<AureusDbContext> options) : DbContext(options)
{
    public DbSet<UserDb> Users => Set<UserDb>();

    public DbSet<WorkspaceDb> Workspaces => Set<WorkspaceDb>();

    public DbSet<WorkspaceMemberDb> WorkspaceMembers => Set<WorkspaceMemberDb>();

    public DbSet<FinancialAccountDb> FinancialAccounts => Set<FinancialAccountDb>();

    public DbSet<CategoryDb> Categories => Set<CategoryDb>();

    public DbSet<TransactionDb> Transactions => Set<TransactionDb>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AureusDbContext).Assembly);
    }
}
