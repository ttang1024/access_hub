using AccessHub.AccessControl.Domain.ReadModel;
using AccessHub.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.AccessControl.Infrastructure.Persistence;

public sealed class AccessControlDbContext(DbContextOptions<AccessControlDbContext> options, IDomainEventDispatcher dispatcher)
    : AccessHubDbContext(options, dispatcher)
{
    public const string SchemaName = "access_control";

    public DbSet<SiteState> Sites => Set<SiteState>();

    public DbSet<DoorState> Doors => Set<DoorState>();

    public DbSet<AccessGroupState> AccessGroups => Set<AccessGroupState>();

    public DbSet<CardholderState> Cardholders => Set<CardholderState>();

    public DbSet<CardState> Cards => Set<CardState>();

    public DbSet<SiteEntry> SiteEntries => Set<SiteEntry>();

    public DbSet<SiteChange> SiteChanges => Set<SiteChange>();

    public DbSet<SiteSyncStatus> SyncStatus => Set<SiteSyncStatus>();

    public DbSet<StaleGrantDetection> StaleGrantDetections => Set<StaleGrantDetection>();

    protected override string Schema => SchemaName;

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccessControlDbContext).Assembly);
}
