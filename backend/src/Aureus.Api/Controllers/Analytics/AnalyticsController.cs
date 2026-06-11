using Aureus.Api.Contracts.Analytics;
using Aureus.Api.Filters;
using Aureus.Domain.Analytics;
using Aureus.UseCases.Analytics.GetBreakdown;
using Aureus.UseCases.Analytics.GetCategoryTimeSeries;
using Aureus.UseCases.Analytics.GetInsights;
using Aureus.UseCases.Analytics.GetSummary;
using Aureus.UseCases.Analytics.GetTimeSeries;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aureus.Api.Controllers.Analytics;

[ValidateWorkspaceMember]
[Route("api/workspaces/{workspaceId:guid}/analytics")]
public sealed class AnalyticsController(ISender sender, IMapper mapper) : ApiControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(IReadOnlyList<CurrencySummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummaryAsync(
        Guid workspaceId,
        [FromQuery] AnalyticsFilterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSummaryQuery(ToFilter(workspaceId, request)), cancellationToken);

        return Ok(mapper.Map<IReadOnlyList<CurrencySummaryResponse>>(result));
    }

    [HttpGet("breakdown")]
    [ProducesResponseType(typeof(IReadOnlyList<BreakdownItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBreakdownAsync(
        Guid workspaceId,
        [FromQuery] BreakdownDimension dimension,
        [FromQuery] AnalyticsFilterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetBreakdownQuery(ToFilter(workspaceId, request), dimension), cancellationToken);

        return Ok(mapper.Map<IReadOnlyList<BreakdownItemResponse>>(result));
    }

    [HttpGet("timeseries")]
    [ProducesResponseType(typeof(IReadOnlyList<TimeSeriesPointResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTimeSeriesAsync(
        Guid workspaceId,
        [FromQuery] TimeInterval interval,
        [FromQuery] AnalyticsFilterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetTimeSeriesQuery(ToFilter(workspaceId, request), interval), cancellationToken);

        return Ok(mapper.Map<IReadOnlyList<TimeSeriesPointResponse>>(result));
    }

    [HttpGet("category-timeseries")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryTimeSeriesPointResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoryTimeSeriesAsync(
        Guid workspaceId,
        [FromQuery] TimeInterval interval,
        [FromQuery] AnalyticsFilterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetCategoryTimeSeriesQuery(ToFilter(workspaceId, request), interval), cancellationToken);

        return Ok(mapper.Map<IReadOnlyList<CategoryTimeSeriesPointResponse>>(result));
    }

    [HttpPost("insights")]
    [ProducesResponseType(typeof(InsightsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInsightsAsync(
        Guid workspaceId,
        [FromBody] InsightsRequest request,
        CancellationToken cancellationToken)
    {
        var answer = await sender.Send(
            new GetInsightsQuery(workspaceId, request.Question, request.From, request.To, request.Language ?? "Russian"),
            cancellationToken);

        return Ok(new InsightsResponse(answer));
    }

    private static AnalyticsFilter ToFilter(Guid workspaceId, AnalyticsFilterRequest request) =>
        new(workspaceId, request.From, request.To, request.AccountIds, request.Type, request.CategoryIds);
}
