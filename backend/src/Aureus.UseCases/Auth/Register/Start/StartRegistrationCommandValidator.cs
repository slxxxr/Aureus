using FluentValidation;
using Aureus.UseCases.Validation;

namespace Aureus.UseCases.Auth.Register.Start;

internal sealed class StartRegistrationCommandValidator : AbstractValidator<StartRegistrationCommand>
{
    public StartRegistrationCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(InputLimits.EmailMaxLength)
            .EmailAddress();
    }
}
