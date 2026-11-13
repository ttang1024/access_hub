using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Infrastructure.Auth;
using AccessHub.Contracts.IntegrationEvents.Cardholder;
using Grpc.Core;

namespace AccessHub.AccessControl.IntegrationTests;

/// <summary>Builds Cardholder events for one site and publishes them through the real broker.</summary>
internal sealed class TestSite(AccessControlFixture fixture)
{
    private readonly Dictionary<Guid, int> _versions = [];

    public Guid SiteId { get; } = Guid.CreateVersion7();

    public Guid DoorId { get; } = Guid.CreateVersion7();

    public Guid GroupId { get; } = Guid.CreateVersion7();

    public string ApiKey { get; private set; } = string.Empty;

    public Metadata Credentials => new() { { AuthConstants.Headers.SiteId, SiteId.ToString() }, { AuthConstants.Headers.ApiKey, ApiKey } };

    public static WeeklyTimeWindow[] AllWeek => Enum.GetValues<DayOfWeek>().Select(d => new WeeklyTimeWindow(d, 0, 1440)).ToArray();

    /// <summary>Registers the site with one door and one group (3 events, 2 site changes: door and group).</summary>
    public async Task<TestSite> RegisterAsync(string timeZone = "UTC")
    {
        var (key, hash) = SiteApiKey.Generate();
        ApiKey = key;
        await fixture.Events.PublishAsync(new SiteRegistered(Guid.NewGuid(), Now, Guid.NewGuid(), SiteId, "Test site", timeZone, Convert.ToBase64String(hash), Next(SiteId)));
        await fixture.Events.PublishAsync(new DoorRegistered(Guid.NewGuid(), Now, Guid.NewGuid(), DoorId, SiteId, "Front", Next(DoorId)));
        await fixture.Events.PublishAsync(GroupEvent());
        await fixture.WaitForSiteVersionAsync(SiteId, 1);
        await fixture.WaitForSequenceAsync(SiteId, 2);
        return this;
    }

    public AccessGroupChanged GroupEvent(params WeeklyTimeWindow[] windows) =>
        new(Guid.NewGuid(), Now, Guid.NewGuid(), GroupId, "Staff", false, [new AccessGroupDoor(DoorId, SiteId, windows.Length == 0 ? AllWeek : windows)], Next(GroupId));

    public CardholderChanged Cardholder(Guid cardholderId, bool active = true, int? version = null, params Guid[] groups) =>
        new(Guid.NewGuid(), Now, Guid.NewGuid(), cardholderId, "Test", "Holder", active, groups.Length == 0 ? [GroupId] : groups, version ?? Next(cardholderId));

    public CardIssued Card(Guid cardId, Guid cardholderId, string number) =>
        new(Guid.NewGuid(), Now, Guid.NewGuid(), cardId, cardholderId, number, null, false, null, Next(cardId));

    public CardRevoked Revoke(Guid cardId, Guid cardholderId, string number, DateTimeOffset? at = null) =>
        new(Guid.NewGuid(), at ?? Now, Guid.NewGuid(), cardId, cardholderId, number, null, true, at ?? Now, Next(cardId));

    /// <summary>Adds a cardholder with one card (2 events, 1 site change: the card).</summary>
    public async Task<(Guid CardholderId, Guid CardId, string Number)> AddCardAsync()
    {
        var (holder, card, number) = (Guid.CreateVersion7(), Guid.CreateVersion7(), NewCardNumber());
        await fixture.Events.PublishAsync(Cardholder(holder));
        await fixture.Events.PublishAsync(Card(card, holder, number));
        return (holder, card, number);
    }

    public static string NewCardNumber() => "T" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

    private static DateTimeOffset Now => DateTimeOffset.UtcNow;

    private int Next(Guid id) => _versions[id] = _versions.GetValueOrDefault(id) + 1;
}
