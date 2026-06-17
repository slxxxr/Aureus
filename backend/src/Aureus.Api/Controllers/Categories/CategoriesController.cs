using Aureus.Api.Contracts.Categories;
using Aureus.Api.Filters;
using Aureus.Domain.Workspaces;
using Aureus.UseCases.Categories.CreateCategory;
using Aureus.UseCases.Categories.DeleteCategory;
using Aureus.UseCases.Categories.GetCategories;
using Aureus.UseCases.Categories.UpdateCategory;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aureus.Api.Controllers.Categories;

[Route("api/workspaces/{workspaceId:guid}/categories")]
public sealed class CategoriesController(ISender sender, IMapper mapper) : ApiControllerBase
{
    [HttpGet]
    [RequireWorkspaceRole(WorkspaceRole.Member)]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var categories = await sender.Send(new GetCategoriesQuery(workspaceId), cancellationToken);

        return Ok(mapper.Map<IReadOnlyList<CategoryResponse>>(categories));
    }

    [HttpPost]
    [RequireWorkspaceRole(WorkspaceRole.Manager)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync(
        Guid workspaceId,
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCategoryCommand(workspaceId, request.Name, request.Type);

        var category = await sender.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, mapper.Map<CategoryResponse>(category));
    }

    [HttpPatch("{categoryId:guid}")]
    [RequireWorkspaceRole(WorkspaceRole.Manager)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(
        Guid workspaceId,
        Guid categoryId,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCategoryCommand(categoryId, workspaceId, request.Name);

        var category = await sender.Send(command, cancellationToken);

        return Ok(mapper.Map<CategoryResponse>(category));
    }

    [HttpDelete("{categoryId:guid}")]
    [RequireWorkspaceRole(WorkspaceRole.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(
        Guid workspaceId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteCategoryCommand(categoryId, workspaceId), cancellationToken);

        return NoContent();
    }
}
