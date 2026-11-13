using AccessHub.BuildingBlocks.Infrastructure.Persistence;
using AccessHub.Cardholder.Domain.AccessGroups;
using AccessHub.Cardholder.Domain.Cards;
using AccessHub.Cardholder.Domain.Doors;
using AccessHub.Cardholder.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using CardholderAggregate = AccessHub.Cardholder.Domain.Cardholders.Cardholder;

namespace AccessHub.Cardholder.Infrastructure.Persistence;

public sealed class CardholderDbContext(DbContextOptions<CardholderDbContext> options, IDomainEventDispatcher dispatcher)
    : AccessHubDbContext(options, dispatcher)
{
    public const string SchemaName = "cardholder";

    public DbSet<Site> Sites => Set<Site>();

    public DbSet<Door> Doors => Set<Door>();

    public DbSet<CardholderAggregate> Cardholders => Set<CardholderAggregate>();

    public DbSet<Card> Cards => Set<Card>();

    public DbSet<AccessGroup> AccessGroups => Set<AccessGroup>();

    protected override string Schema => SchemaName;

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CardholderDbContext).Assembly);
}
