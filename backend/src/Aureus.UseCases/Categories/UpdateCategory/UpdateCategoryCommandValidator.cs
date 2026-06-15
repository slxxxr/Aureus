using FluentValidation;
using Aureus.UseCases.Validation;

namespace Aureus.UseCases.Categories.UpdateCategory;

internal sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("'Name' must not be empty.")
            .MaximumLength(InputLimits.CategoryNameMaxLength);
    }
}
