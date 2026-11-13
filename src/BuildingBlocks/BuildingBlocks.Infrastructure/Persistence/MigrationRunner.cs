using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AccessHub.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Applies migrations at startup in Development only. Elsewhere, <c>--migrate</c> applies them and exits
/// (used by the Kubernetes migration Job).
/// </summary>
public static class MigrationRunner
{
    /// <summary>The command-line flag that applies migrations and exits.</summary>
    public const string MigrateFlag = "--migrate";

    /// <summary>Returns true when the process should exit because it only ran migrations.</summary>
    public static async Task<bool> ApplyMigrationsAsync<TContext>(this WebApplication app, string[] args)
        where TContext : DbContext
    {
        var migrateOnly = args.Contains(MigrateFlag, StringComparer.Ordinal);
        if (!migrateOnly && !app.Environment.IsDevelopment())
        {
            return false;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        await db.Database.MigrateAsync(app.Lifetime.ApplicationStopping);
        return migrateOnly;
    }
}
