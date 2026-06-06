using Aureus.Domain.FinancialAccounts;
using Aureus.UseCases.Common.Persistence;
using MediatR;

namespace Aureus.UseCases.FinancialAccounts.UpdateFinancialAccount;

public sealed class UpdateFinancialAccountHandler(IFinancialAccountRepository repository)
    : IRequestHandler<UpdateFinancialAccountCommand, FinancialAccount>
{
    public async Task<FinancialAccount> Handle(
        UpdateFinancialAccountCommand command,
        CancellationToken cancellationToken)
    {
        var account = await repository.FindByIdAsync(command.AccountId, command.WorkspaceId, cancellationToken);

        if (account is null)
        {
            throw new FinancialAccountException(
                FinancialAccountErrorCode.NotFound,
                $"Financial account {command.AccountId} not found.");
        }

        if (command.Name is not null)
        {
            account.Name = command.Name.Trim();
        }

        if (command.InitialBalanceMinor is not null)
        {
            var delta = command.InitialBalanceMinor.Value - account.InitialBalanceMinor;
            account.InitialBalanceMinor = command.InitialBalanceMinor.Value;
            account.CurrentBalanceMinor += delta;
        }

        account.UpdatedAt = DateTimeOffset.UtcNow;

        await repository.UpdateAsync(account, cancellationToken);

        return account;
    }
}
