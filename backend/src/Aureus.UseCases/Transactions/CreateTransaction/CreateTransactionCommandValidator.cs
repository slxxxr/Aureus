using FluentValidation;
using Aureus.UseCases.Validation;

namespace Aureus.UseCases.Transactions.CreateTransaction;

internal sealed class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(InputLimits.TransactionNameMaxLength);

        RuleFor(x => x.Note)
            .MaximumLength(InputLimits.TransactionNoteMaxLength)
            .When(x => x.Note is not null);

        RuleFor(x => x.AmountMinor)
            .GreaterThan(0)
            .LessThanOrEqualTo(InputLimits.TransactionMaxAmountMinor);
    }
}
