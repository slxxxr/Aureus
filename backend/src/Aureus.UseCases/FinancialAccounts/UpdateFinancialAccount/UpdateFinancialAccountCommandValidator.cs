using FluentValidation;
using Aureus.UseCases.Validation;

namespace Aureus.UseCases.FinancialAccounts.UpdateFinancialAccount;

internal sealed class UpdateFinancialAccountCommandValidator : AbstractValidator<UpdateFinancialAccountCommand>
{
    public UpdateFinancialAccountCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(InputLimits.AccountNameMaxLength)
            .When(x => x.Name is not null);

        RuleFor(x => x.InitialBalanceMinor)
            .GreaterThanOrEqualTo(0)
            .When(x => x.InitialBalanceMinor is not null);
    }
}
