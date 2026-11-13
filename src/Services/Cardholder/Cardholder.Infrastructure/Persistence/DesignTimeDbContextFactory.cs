using AccessHub.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AccessHub.Cardholder.Infrastructure.Persistence;

/// <summary>Used by <c>dotnet ef</c> only. The connection string is a placeholder: generating migrations needs no database.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CardholderDbContext>
{
    public CardholderDbContext CreateDbContext(string[] args) =>
        new(
            new DbContextOptionsBuilder<CardholderDbContext>()
                .UseNpgsql("Host=localhost;Database=cardholder_design", o => o.MigrationsHistoryTable("__ef_migrations_history", CardholderDbContext.SchemaName))
                .Options,
            new NoOpDomainEventDispatcher());
}
