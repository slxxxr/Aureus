using System.ComponentModel.DataAnnotations;

namespace Aureus.Api.Contracts.Workspaces;

public sealed record InviteMemberRequest([Required] string Email, string? Language);
