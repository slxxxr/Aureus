using FluentValidation;
using Aureus.UseCases.Validation;

namespace Aureus.UseCases.Transactions.UpdateTransaction;

internal sealed class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(InputLimits.TransactionNameMaxLength)
            .When(x => x.Name is not null);

        RuleFor(x => x.Note)
            .MaximumLength(InputLimits.TransactionNoteMaxLength)
            .When(x => x.Note is not null);

        RuleFor(x => x.AmountMinor)
            .GreaterThan(0)
            .LessThanOrEqualTo(InputLimits.TransactionMaxAmountMinor)
            .When(x => x.AmountMinor is not null);
    }
}
