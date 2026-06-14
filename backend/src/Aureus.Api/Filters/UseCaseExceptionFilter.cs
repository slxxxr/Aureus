using Aureus.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aureus.Api.Filters;

public sealed class UseCaseExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            var problem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "ValidationFailed",
                Instance = context.HttpContext.Request.Path
            };

            context.Result = new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
            context.ExceptionHandled = true;
            return;
        }

        if (context.Exception is not DomainException exception)
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

        var domainProblem = new ProblemDetails
        {
            Status = statusCode,
            Title = exception.ErrorCode,
            Detail = exception.Message,
            Instance = context.HttpContext.Request.Path
        };

        context.Result = new ObjectResult(domainProblem) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
