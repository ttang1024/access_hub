using System.Diagnostics;
using AccessHub.BuildingBlocks.Application;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AccessHub.BuildingBlocks.Infrastructure.Web;

/// <summary>Turns unhandled exceptions into a generic 500 ProblemDetails, logging the exception with the trace ID.</summary>
public sealed partial class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
        if (exception is ConcurrencyConflictException conflict)
        {
            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            return await problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflict",
                    Detail = conflict.Message,
                    Extensions = { ["traceId"] = traceId, ["code"] = "Concurrency.Conflict" },
                },
            });
        }

        LogUnhandled(logger, exception, traceId);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Extensions = { ["traceId"] = traceId },
            },
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception (trace {TraceId})")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string traceId);
}
