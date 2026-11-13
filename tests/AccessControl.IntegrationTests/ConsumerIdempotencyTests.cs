using AccessHub.AccessControl.Application;
using AccessHub.AccessControl.Infrastructure;
using AccessHub.AccessControl.Infrastructure.Persistence;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Infrastructure.Messaging;
using AccessHub.BuildingBlocks.Infrastructure.Persistence;
using AccessHub.Contracts.IntegrationEvents.Cardholder;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AccessHub.AccessControl.IntegrationTests;

/// <summary>
/// Runs the real consumers on the MassTransit test harness (in-memory transport) with the real EF inbox and Postgres,
/// to prove each event is applied once however it is redelivered.
/// </summary>
public sealed class ConsumerIdempotencyTests(AccessControlFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset T0 = new(2026, 5, 1, 9, 0, 0, TimeSpan.Zero);
    private readonly Guid _site = Guid.CreateVersion7();
    private readonly Guid _door = Guid.CreateVersion7();
    private readonly Guid _group = Guid.CreateVersion7();
    private readonly Guid _holder = Guid.CreateVersion7();
    private readonly Guid _card = Guid.CreateVersion7();
    private readonly Guid _sentinel = Guid.CreateVersion7();
    private readonly string _sentinelHash = Convert.ToBase64String(SiteApiKey.Generate().Hash);
    private int _sentinelVersion;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    public async ValueTask InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMetrics();
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<AccessControlDbContext>(o => o.UseNpgsql(fixture.PostgresConnectionString));
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();
        services.AddAccessControlApplication();
        services.AddAccessControlStores();
        services.AddMassTransitTestHarness(x =>
        {
            x.ConfigureAccessHubBus<AccessControlDbContext>("idempotency-test");
            AccessControlInfrastructureRegistration.AddConsumers(x);

            // One message at a time, so a sentinel processed after a message proves that message (and its retries) finished.
            x.AddConfigureEndpointsCallback((_, _, cfg) => cfg.ConcurrentMessageLimit = 1);
        });
        _provider = services.BuildServiceProvider(validateScopes: true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();

        var (_, hash) = SiteApiKey.Generate();
        await PublishAsync(new SiteRegistered(Guid.NewGuid(), T0, Guid.NewGuid(), _site, "S", "UTC", Convert.ToBase64String(hash), 1));
        await PublishAsync(new DoorRegistered(Guid.NewGuid(), T0, Guid.NewGuid(), _door, _site, "D", 1));
        await PublishAsync(new AccessGroupChanged(Guid.NewGuid(), T0, Guid.NewGuid(), _group, "G", false, [new AccessGroupDoor(_door, _site, TestSite.AllWeek)], 1));
        await PublishAsync(Holder(active: true, version: 1));
        await PublishAsync(new CardIssued(Guid.NewGuid(), T0, Guid.NewGuid(), _card, _holder, "IDEM" + _card.ToString("N")[..8].ToUpperInvariant(), null, false, null, 1));
        (await ChangeCountAsync()).ShouldBe(3);
    }

    public async ValueTask DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task SameMessageDeliveredTwice_InboxApplies_Once()
    {
        var messageId = Guid.NewGuid();
        var revoked = Revoke(version: 2);

        await PublishAsync(revoked, messageId);
        await PublishAsync(revoked, messageId);

        (await ChangeCountAsync()).ShouldBe(4);
        (await CardVersionAsync()).ShouldBe(2);
        (await _harness.Published.Any<Fault>(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task SameEventWithNewMessageId_VersionCheck_AppendsNoExtraChange()
    {
        var revoked = Revoke(version: 2);

        await PublishAsync(revoked);
        await PublishAsync(revoked);
        await PublishAsync(revoked with { EventId = Guid.NewGuid() });

        (await ChangeCountAsync()).ShouldBe(4);
    }

    [Fact]
    public async Task VersionsOutOfOrder_NewestWins_OlderAppendsNothing()
    {
        await PublishAsync(Holder(active: false, version: 3));
        var afterNewest = await ChangeCountAsync();

        await PublishAsync(Holder(active: true, version: 2));

        (await ChangeCountAsync()).ShouldBe(afterNewest);
        var payload = await fixture.WithDbAsync(db => db.SiteEntries.AsNoTracking().Where(e => e.SiteId == _site && e.EntityId == _card).Select(e => e.Payload).SingleAsync());
        payload.ShouldContain("\"cardholderActive\":false");
        var holder = await fixture.WithDbAsync(db => db.Cardholders.AsNoTracking().SingleAsync(c => c.CardholderId == _holder));
        (holder.Version, holder.IsActive).ShouldBe((3, false));
    }

    private CardholderChanged Holder(bool active, int version) =>
        new(Guid.NewGuid(), T0.AddMinutes(version), Guid.NewGuid(), _holder, "A", "B", active, [_group], version);

    private CardRevoked Revoke(int version) =>
        new(Guid.NewGuid(), T0.AddHours(1), Guid.NewGuid(), _card, _holder, "IDEM" + _card.ToString("N")[..8].ToUpperInvariant(), null, true, T0.AddHours(1), version);

    private async Task PublishAsync<T>(T message, Guid? messageId = null)
        where T : class
    {
        await _harness.Bus.Publish(message, ctx =>
        {
            if (messageId is { } id)
            {
                ctx.MessageId = id;
            }
        }, TestContext.Current.CancellationToken);

        var version = ++_sentinelVersion;
        await _harness.Bus.Publish(new SiteRegistered(Guid.NewGuid(), T0, Guid.NewGuid(), _sentinel, "sentinel", "UTC", _sentinelHash, version), TestContext.Current.CancellationToken);
        await WaitUntilAsync(async () => await fixture.WithDbAsync(db => db.Sites.Where(x => x.SiteId == _sentinel).Select(x => x.Version).FirstOrDefaultAsync()) >= version);
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (!await condition())
        {
            await Task.Delay(50, cts.Token);
        }
    }

    private Task<int> ChangeCountAsync() => fixture.WithDbAsync(db => db.SiteChanges.CountAsync(c => c.SiteId == _site));

    private Task<int> CardVersionAsync() => fixture.WithDbAsync(db => db.Cards.Where(c => c.CardId == _card).Select(c => c.Version).SingleAsync());
}
