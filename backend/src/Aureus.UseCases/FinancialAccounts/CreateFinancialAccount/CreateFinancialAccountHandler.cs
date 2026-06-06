using Aureus.Domain.FinancialAccounts;
using Aureus.UseCases.Common.Persistence;
using MediatR;

namespace Aureus.UseCases.FinancialAccounts.CreateFinancialAccount;

public sealed class CreateFinancialAccountHandler(IFinancialAccountRepository repository)
    : IRequestHandler<CreateFinancialAccountCommand, FinancialAccount>
{
    public async Task<FinancialAccount> Handle(
        CreateFinancialAccountCommand command,
        CancellationToken cancellationToken)
    {
        var account = new FinancialAccount
        {
            Id = Guid.NewGuid(),
            WorkspaceId = command.WorkspaceId,
            Name = command.Name.Trim(),
            Currency = command.Currency,
            InitialBalanceMinor = command.InitialBalanceMinor,
            CurrentBalanceMinor = command.InitialBalanceMinor,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await repository.AddAsync(account, cancellationToken);

        return account;
    }
}
