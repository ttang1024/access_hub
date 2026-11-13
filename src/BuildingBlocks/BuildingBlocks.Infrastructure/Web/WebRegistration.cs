using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AccessHub.BuildingBlocks.Infrastructure.Web;

/// <summary>ProblemDetails, the global exception handler and the TimeProvider.</summary>
public static class WebRegistration
{
    /// <summary>Registers RFC 9457 ProblemDetails (never including exception details) and the global exception handler.</summary>
    public static IHostApplicationBuilder AddAccessHubWebDefaults(this IHostApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
        {
            ctx.ProblemDetails.Instance ??= ctx.HttpContext.Request.Path;
            ctx.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.TraceId.ToString() ?? ctx.HttpContext.TraceIdentifier);
            ctx.ProblemDetails.Extensions.Remove("exception");
        });
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddSingleton(TimeProvider.System);
        return builder;
    }
}
