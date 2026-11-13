using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Infrastructure.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AccessHub.BuildingBlocks.Infrastructure.Persistence;

/// <summary>Registers a service's DbContext with Npgsql, the unit of work, audit log and DB health check.</summary>
public static class PersistenceRegistration
{
    /// <summary>Default upper bound of each service instance's connection pool.</summary>
    public const int DefaultMaxPoolSize = 50;

    /// <summary>Registers <typeparamref name="TContext"/> against the named connection string.</summary>
    public static IHostApplicationBuilder AddAccessHubPersistence<TContext>(
        this IHostApplicationBuilder builder, string connectionName, string schema)
        where TContext : AccessHubDbContext
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionName)
            ?? throw new InvalidOperationException($"Connection string '{connectionName}' is not configured.");

        // Outbox inserts are hundreds of identical statements per batch; auto-preparing them skips repeated parse/plan.
        // The pool is bounded so replicas cannot exhaust the server's connections (an explicit value in the
        // connection string wins).
        var npgsql = new Npgsql.NpgsqlConnectionStringBuilder(connectionString) { MaxAutoPrepare = 50, AutoPrepareMinUsages = 2 };
        if (!connectionString.Contains("Maximum Pool Size", StringComparison.OrdinalIgnoreCase) && !connectionString.Contains("MaxPoolSize", StringComparison.OrdinalIgnoreCase))
        {
            npgsql.MaxPoolSize = DefaultMaxPoolSize;
        }

        connectionString = npgsql.ConnectionString;
        builder.Services.AddDbContext<TContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", schema)));
        builder.Services.AddScoped<AccessHubDbContext>(sp => sp.GetRequiredService<TContext>());
        builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TContext>());
        builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        builder.Services.AddScoped<IAuditLog, AuditLog>();
        builder.Services.AddHealthChecks().AddDbContextCheck<TContext>("database", tags: ["ready"]);
        return builder;
    }
}
