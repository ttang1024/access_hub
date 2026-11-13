using AccessHub.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AccessHub.EventIngestion.Infrastructure.Persistence;

/// <summary>Used by <c>dotnet ef</c> only; generating migrations needs no database.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IngestionDbContext>
{
    public IngestionDbContext CreateDbContext(string[] args) =>
        new(
            new DbContextOptionsBuilder<IngestionDbContext>()
                .UseNpgsql("Host=localhost;Database=ingestion_design", o => o.MigrationsHistoryTable("__ef_migrations_history", IngestionDbContext.SchemaName))
                .Options,
            new NoOpDomainEventDispatcher());
}
