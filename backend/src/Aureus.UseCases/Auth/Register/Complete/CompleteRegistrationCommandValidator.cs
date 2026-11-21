using Aureus.UseCases.Validation;
using FluentValidation;

namespace Aureus.UseCases.Auth.Register.Complete;

internal sealed class CompleteRegistrationCommandValidator : AbstractValidator<CompleteRegistrationCommand>
{
    public CompleteRegistrationCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("'Name' must not be empty.")
            .MaximumLength(InputLimits.NameMaxLength);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(InputLimits.PasswordMaxLength);
    }
}