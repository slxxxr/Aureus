using FluentValidation;
using Aureus.UseCases.Validation;

namespace Aureus.UseCases.Workspaces.UpdateWorkspace;

internal sealed class UpdateWorkspaceCommandValidator : AbstractValidator<UpdateWorkspaceCommand>
{
    public UpdateWorkspaceCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(InputLimits.WorkspaceNameMaxLength);
    }
}
