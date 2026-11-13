using AccessHub.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AccessHub.AccessControl.Infrastructure.Persistence;

/// <summary>Used by <c>dotnet ef</c> only; generating migrations needs no database.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AccessControlDbContext>
{
    public AccessControlDbContext CreateDbContext(string[] args) =>
        new(
            new DbContextOptionsBuilder<AccessControlDbContext>()
                .UseNpgsql("Host=localhost;Database=access_control_design", o => o.MigrationsHistoryTable("__ef_migrations_history", AccessControlDbContext.SchemaName))
                .Options,
            new NoOpDomainEventDispatcher());
}
