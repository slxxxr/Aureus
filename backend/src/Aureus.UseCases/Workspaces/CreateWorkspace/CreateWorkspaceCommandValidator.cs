using FluentValidation;
using Aureus.UseCases.Validation;

namespace Aureus.UseCases.Workspaces.CreateWorkspace;

internal sealed class CreateWorkspaceCommandValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("'Name' must not be empty.")
            .MaximumLength(InputLimits.WorkspaceNameMaxLength);
    }
}
