using AccessHub.Alarm.Infrastructure.Sagas;
using AccessHub.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.Alarm.Infrastructure.Persistence;

public sealed class AlarmDbContext(DbContextOptions<AlarmDbContext> options, IDomainEventDispatcher dispatcher)
    : AccessHubDbContext(options, dispatcher)
{
    public const string SchemaName = "alarm";

    public DbSet<AlarmState> Alarms => Set<AlarmState>();

    public DbSet<RecentDenial> RecentDenials => Set<RecentDenial>();

    protected override string Schema => SchemaName;

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        new AlarmStateMap().Configure(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AlarmDbContext).Assembly);
    }
}
