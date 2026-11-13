using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.BuildingBlocks.Infrastructure.Auditing;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Base DbContext for every service. It is the unit of work: <see cref="SaveChangesAsync(bool, CancellationToken)"/>
/// dispatches domain events <em>before</em> saving, so integration events published by their handlers are added
/// to the MassTransit outbox tables and committed in the same transaction as the state change and audit entries.
/// </summary>
public abstract class AccessHubDbContext : DbContext, IUnitOfWork
{
    private const int MaxDispatchRounds = 10;
    private readonly IDomainEventDispatcher _dispatcher;

    /// <summary>Creates the context.</summary>
    protected AccessHubDbContext(DbContextOptions options, IDomainEventDispatcher dispatcher)
        : base(options) => _dispatcher = dispatcher;

    /// <summary>The Postgres schema that holds this service's tables.</summary>
    protected abstract string Schema { get; }

    /// <summary>Audit trail of every state-changing command.</summary>
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    /// <inheritdoc />
    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) => await SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        var strategy = Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async ct =>
        {
            await using var transaction = await Database.BeginTransactionAsync(ct);
            var result = await operation(ct);
            await SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(cancellationToken);
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("The resource was changed by someone else. Reload and retry.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        {
            throw new ConcurrencyConflictException("A resource with the same unique value already exists.", ex);
        }
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new NotSupportedException("Use SaveChangesAsync so domain events are dispatched.");

    /// <inheritdoc />
    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new AuditEntryConfiguration());
        modelBuilder.AddInboxStateEntity(b => b.ToTable("inbox_state"));
        modelBuilder.AddOutboxMessageEntity(b => b.ToTable("outbox_message"));
        modelBuilder.AddOutboxStateEntity(b => b.ToTable("outbox_state"));
        ConfigureModel(modelBuilder);
        SnakeCaseNaming.Apply(modelBuilder);
    }

    /// <summary>Applies the service's entity configurations.</summary>
    protected abstract void ConfigureModel(ModelBuilder modelBuilder);

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        // Handlers may change aggregates and raise further events, so dispatch until quiet (bounded to catch cycles).
        for (var round = 0; round < MaxDispatchRounds; round++)
        {
            var aggregates = ChangeTracker.Entries<IAggregateRoot>()
                .Select(e => e.Entity)
                .Where(a => a.DomainEvents.Count > 0)
                .ToList();
            if (aggregates.Count == 0)
            {
                return;
            }

            var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
            aggregates.ForEach(a => a.ClearDomainEvents());
            foreach (var domainEvent in events)
            {
                await _dispatcher.DispatchAsync(domainEvent, cancellationToken);
            }
        }

        throw new InvalidOperationException($"Domain events were still being raised after {MaxDispatchRounds} dispatch rounds.");
    }
}
