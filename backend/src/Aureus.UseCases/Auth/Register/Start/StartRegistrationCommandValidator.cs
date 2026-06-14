using System.Text.RegularExpressions;
using FluentValidation;
using Aureus.UseCases.Validation;

namespace Aureus.UseCases.Auth.Register.Start;

internal sealed class StartRegistrationCommandValidator : AbstractValidator<StartRegistrationCommand>
{
    private static readonly Regex EmailRegex = new(
        @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public StartRegistrationCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(InputLimits.EmailMaxLength)
            .Matches(EmailRegex);
    }
}
