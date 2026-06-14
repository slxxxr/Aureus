using FluentValidation;
using Aureus.UseCases.Validation;

namespace Aureus.UseCases.Workspaces.CreateWorkspace;

internal sealed class CreateWorkspaceCommandValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(InputLimits.WorkspaceNameMaxLength);
    }
}
