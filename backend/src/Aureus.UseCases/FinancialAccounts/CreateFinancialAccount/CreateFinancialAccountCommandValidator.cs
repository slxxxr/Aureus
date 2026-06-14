using FluentValidation;
using Aureus.UseCases.Validation;

namespace Aureus.UseCases.FinancialAccounts.CreateFinancialAccount;

internal sealed class CreateFinancialAccountCommandValidator : AbstractValidator<CreateFinancialAccountCommand>
{
    private const int CurrencyCodeLength = 3;

    public CreateFinancialAccountCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(InputLimits.AccountNameMaxLength);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(CurrencyCodeLength);

        RuleFor(x => x.InitialBalanceMinor)
            .GreaterThanOrEqualTo(0);
    }
}
