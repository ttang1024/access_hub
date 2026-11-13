using AccessHub.BuildingBlocks.Infrastructure.Persistence;
using AccessHub.EventIngestion.Domain.Events;
using AccessHub.EventIngestion.Domain.Sites;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.EventIngestion.Infrastructure.Persistence;

public sealed class IngestionDbContext(DbContextOptions<IngestionDbContext> options, IDomainEventDispatcher dispatcher)
    : AccessHubDbContext(options, dispatcher)
{
    public const string SchemaName = "ingestion";

    public DbSet<DoorEvent> DoorEvents => Set<DoorEvent>();

    public DbSet<KnownSite> Sites => Set<KnownSite>();

    protected override string Schema => SchemaName;

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IngestionDbContext).Assembly);
}
