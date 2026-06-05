using Aureus.UseCases.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aureus.Api.Filters;

public sealed class UseCaseExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not UseCaseException exception)
        {
            return;
        }

        var statusCode = exception.ErrorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = exception.ErrorCode,
            Detail = exception.Message,
            Instance = context.HttpContext.Request.Path
        };

        context.Result = new ObjectResult(problem) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
