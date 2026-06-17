using FluentValidation;
using Aureus.UseCases.Validation;

namespace Aureus.UseCases.Workspaces.InviteMember;

internal sealed class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(InputLimits.EmailMaxLength)
            .Matches(EmailRegex.Pattern);
    }
}
