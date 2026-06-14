using FluentValidation;
using Aureus.UseCases.Validation;

namespace Aureus.UseCases.Auth.Login;

internal sealed class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(InputLimits.EmailMaxLength)
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(InputLimits.PasswordMaxLength);
    }
}
