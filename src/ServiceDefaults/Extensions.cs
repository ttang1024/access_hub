using AccessHub.ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

#pragma warning disable IDE0130 // Aspire convention: extension methods live in Microsoft.Extensions.Hosting so they are found without a using.
namespace Microsoft.Extensions.Hosting;
#pragma warning restore IDE0130

/// <summary>Defaults shared by every AccessHub process: OpenTelemetry, health checks, service discovery and resilience.</summary>
public static class Extensions
{
    /// <summary>Liveness endpoint path.</summary>
    public const string LivenessPath = "/health/live";

    /// <summary>Readiness endpoint path.</summary>
    public const string ReadinessPath = "/health/ready";

    /// <summary>Health check tag for readiness checks (database, broker).</summary>
    public const string ReadyTag = "ready";

    /// <summary>Adds OpenTelemetry, default health checks, service discovery and a standard resilience pipeline on HttpClients.</summary>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });
        return builder;
    }

    /// <summary>Configures traces, metrics and logs, exported over OTLP when an endpoint is configured.</summary>
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(AccessHubTelemetry.Name, AccessHubTelemetry.MassTransitSource))
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName, AccessHubTelemetry.Name, AccessHubTelemetry.MassTransitSource)
                .AddAspNetCoreInstrumentation(o => o.Filter = ctx =>
                    !ctx.Request.Path.StartsWithSegments(LivenessPath, StringComparison.OrdinalIgnoreCase) &&
                    !ctx.Request.Path.StartsWithSegments(ReadinessPath, StringComparison.OrdinalIgnoreCase))
                .AddHttpClientInstrumentation()
                .AddNpgsql());

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    /// <summary>Maps <c>/health/live</c> (process up) and <c>/health/ready</c> (database and broker reachable).</summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks(LivenessPath, new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") }).AllowAnonymous();
        app.MapHealthChecks(ReadinessPath, new HealthCheckOptions { Predicate = r => r.Tags.Contains(ReadyTag) }).AllowAnonymous();
        return app;
    }
}
