using Aureus.UseCases.Validation;
using FluentValidation;

namespace Aureus.UseCases.Auth.Login;

internal sealed class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(InputLimits.EmailMaxLength)
            .Matches(EmailRegex.Pattern);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(InputLimits.PasswordMaxLength);
    }
}