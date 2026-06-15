using Aureus.UseCases.Validation;
using FluentValidation;

namespace Aureus.UseCases.Profile.UpdateProfile;

internal sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("'Name' must not be empty.")
            .MaximumLength(InputLimits.NameMaxLength);
    }
}
