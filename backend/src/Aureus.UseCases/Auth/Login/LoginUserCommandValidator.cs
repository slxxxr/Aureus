using System.Text.RegularExpressions;
using FluentValidation;
using Aureus.UseCases.Validation;

namespace Aureus.UseCases.Auth.Login;

internal sealed class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    private static readonly Regex EmailRegex = new(
        @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(InputLimits.EmailMaxLength)
            .Matches(EmailRegex);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(InputLimits.PasswordMaxLength);
    }
}
