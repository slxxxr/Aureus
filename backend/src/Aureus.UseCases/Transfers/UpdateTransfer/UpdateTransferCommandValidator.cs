using FluentValidation;
using Aureus.UseCases.Validation;

namespace Aureus.UseCases.Transfers.UpdateTransfer;

internal sealed class UpdateTransferCommandValidator : AbstractValidator<UpdateTransferCommand>
{
    public UpdateTransferCommandValidator()
    {
        RuleFor(x => x.Note)
            .MaximumLength(InputLimits.TransactionNoteMaxLength)
            .When(x => x.Note is not null);

        RuleFor(x => x.AmountMinor)
            .GreaterThan(0)
            .LessThanOrEqualTo(InputLimits.TransactionMaxAmountMinor)
            .When(x => x.AmountMinor is not null);
    }
}
