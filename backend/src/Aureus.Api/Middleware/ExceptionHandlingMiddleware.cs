using Aureus.UseCases.Auth.Register;
using Microsoft.AspNetCore.Mvc;

namespace Aureus.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (RegistrationException exception)
        {
            await HandleRegistrationExceptionAsync(context, exception);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled API exception occurred.");

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while processing the request.",
                Instance = context.Request.Path
            };

            await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
        }
    }

    private static async Task HandleRegistrationExceptionAsync(HttpContext context, RegistrationException exception)
    {
        if (context.Response.HasStarted)
        {
            throw exception;
        }

        var statusCode = exception.Code == RegistrationErrorCode.EmailAlreadyExists
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status400BadRequest;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = exception.Code.ToString(),
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
    }
}
