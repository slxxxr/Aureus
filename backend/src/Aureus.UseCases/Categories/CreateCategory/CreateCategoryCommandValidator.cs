using FluentValidation;
using Aureus.UseCases.Validation;

namespace Aureus.UseCases.Categories.CreateCategory;

internal sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(InputLimits.CategoryNameMaxLength);
    }
}
