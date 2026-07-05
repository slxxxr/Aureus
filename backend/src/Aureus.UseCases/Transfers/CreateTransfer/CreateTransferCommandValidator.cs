using FluentValidation;
using Aureus.UseCases.Validation;

namespace Aureus.UseCases.Transfers.CreateTransfer;

internal sealed class CreateTransferCommandValidator : AbstractValidator<CreateTransferCommand>
{
    public CreateTransferCommandValidator()
    {
        RuleFor(x => x.ToAccountId)
            .NotEqual(x => x.FromAccountId)
            .WithMessage("'ToAccountId' must not equal 'FromAccountId'.");

        RuleFor(x => x.Note)
            .MaximumLength(InputLimits.TransactionNoteMaxLength)
            .When(x => x.Note is not null);

        RuleFor(x => x.AmountMinor)
            .GreaterThan(0)
            .LessThanOrEqualTo(InputLimits.TransactionMaxAmountMinor);
    }
}
