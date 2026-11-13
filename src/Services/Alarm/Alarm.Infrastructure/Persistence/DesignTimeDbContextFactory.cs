using AccessHub.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AccessHub.Alarm.Infrastructure.Persistence;

/// <summary>Used by <c>dotnet ef</c> only; generating migrations needs no database.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AlarmDbContext>
{
    public AlarmDbContext CreateDbContext(string[] args) =>
        new(
            new DbContextOptionsBuilder<AlarmDbContext>()
                .UseNpgsql("Host=localhost;Database=alarm_design", o => o.MigrationsHistoryTable("__ef_migrations_history", AlarmDbContext.SchemaName))
                .Options,
            new NoOpDomainEventDispatcher());
}
