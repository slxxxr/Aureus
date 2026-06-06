using System.ComponentModel.DataAnnotations;

namespace Aureus.Api.Contracts.Workspaces;

public sealed record CreateWorkspaceRequest([Required] string Name);
