using Microsoft.AspNetCore.Mvc;

namespace Aureus.Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ValidateWorkspaceMemberAttribute() : TypeFilterAttribute(typeof(ValidateWorkspaceMemberFilter));
