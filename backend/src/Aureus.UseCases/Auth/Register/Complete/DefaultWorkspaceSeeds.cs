using Aureus.Domain.Categories;
using Aureus.Domain.FinancialAccounts;
using Aureus.Domain.Transactions;
using Aureus.Domain.Workspaces;

namespace Aureus.UseCases.Auth.Register.Complete;

internal sealed record DefaultWorkspaceBundle(
    Workspace Workspace,
    WorkspaceMember Member,
    IReadOnlyList<FinancialAccount> Accounts,
    IReadOnlyList<Category> Categories);

internal static class DefaultWorkspaceSeeds
{
    private static readonly Dictionary<string, SeedData> Seeds = new()
    {
        ["ru"] = new SeedData(
            "Личное",
            "RUB",
            ["Наличные", "Карта"],
            ["Продукты", "Кафе и рестораны", "Транспорт", "Жильё", "Здоровье", "Развлечения", "Одежда", "Связь"],
            ["Зарплата", "Прочее"]),

        ["en"] = new SeedData(
            "Personal",
            "USD",
            ["Cash", "Debit Card"],
            [
                "Groceries", "Dining Out", "Transport", "Housing", "Health", "Entertainment", "Clothing",
                "Communications"
            ],
            ["Salary", "Other"])
    };

    private static readonly SeedData Fallback = Seeds["ru"];

    public static DefaultWorkspaceBundle Build(Guid userId, string? language, DateTimeOffset now)
    {
        var data = Seeds.GetValueOrDefault(language ?? string.Empty, Fallback);
        var workspaceId = Guid.NewGuid();

        var workspace = new Workspace
        {
            Id = workspaceId,
            Name = data.WorkspaceName,
            CreatedAt = now
        };

        var member = new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = WorkspaceRole.Owner,
            JoinedAt = now
        };

        return new DefaultWorkspaceBundle(workspace, member, BuildAccounts(workspaceId, data, now),
            BuildCategories(workspaceId, data, now));
    }

    private static List<FinancialAccount> BuildAccounts(Guid workspaceId, SeedData data, DateTimeOffset now)
    {
        return data.AccountNames.Select(name => new FinancialAccount
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = name,
            Currency = data.Currency,
            InitialBalanceMinor = 0,
            CurrentBalanceMinor = 0,
            CreatedAt = now
        }).ToList();
    }

    private static List<Category> BuildCategories(Guid workspaceId, SeedData data, DateTimeOffset now)
    {
        var categories = data.ExpenseNames.Select(name => new Category
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = name,
            Type = TransactionType.Expense,
            CreatedAt = now
        }).ToList();

        categories.AddRange(data.IncomeNames.Select(name => new Category
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = name,
            Type = TransactionType.Income,
            CreatedAt = now
        }));

        return categories;
    }

    private sealed record SeedData(
        string WorkspaceName,
        string Currency,
        string[] AccountNames,
        string[] ExpenseNames,
        string[] IncomeNames);
}