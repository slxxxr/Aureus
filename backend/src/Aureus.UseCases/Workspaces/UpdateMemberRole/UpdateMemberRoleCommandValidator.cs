using Aureus.Domain.Workspaces;
using FluentValidation;

namespace Aureus.UseCases.Workspaces.UpdateMemberRole;

internal sealed class UpdateMemberRoleCommandValidator : AbstractValidator<UpdateMemberRoleCommand>
{
    public UpdateMemberRoleCommandValidator()
    {
        RuleFor(x => x.NewRole)
            .Must(r => r is WorkspaceRole.Member or WorkspaceRole.Manager)
            .WithMessage("Role must be Member or Manager.");
    }
}
