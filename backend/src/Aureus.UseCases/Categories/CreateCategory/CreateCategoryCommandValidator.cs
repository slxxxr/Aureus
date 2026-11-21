using Aureus.UseCases.Validation;
using FluentValidation;

namespace Aureus.UseCases.Categories.CreateCategory;

internal sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("'Name' must not be empty.")
            .MaximumLength(InputLimits.CategoryNameMaxLength);
    }
}