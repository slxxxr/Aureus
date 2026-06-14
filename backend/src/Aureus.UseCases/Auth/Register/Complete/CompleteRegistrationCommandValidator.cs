using FluentValidation;
using Aureus.UseCases.Validation;

namespace Aureus.UseCases.Auth.Register.Complete;

internal sealed class CompleteRegistrationCommandValidator : AbstractValidator<CompleteRegistrationCommand>
{
    public CompleteRegistrationCommandValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(InputLimits.PasswordMaxLength);
    }
}
