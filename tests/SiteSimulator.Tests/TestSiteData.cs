using System.Diagnostics.Metrics;
using AccessHub.Contracts.Grpc.Sync.V1;
using AccessHub.SiteSimulator.Storage;

namespace AccessHub.SiteSimulator.Tests;

/// <summary>A site with one door, a 24/7 group, a valid card and a revoked card, persisted in a temp SQLite file.</summary>
internal sealed class TestSiteData : IDisposable
{
    public const string ValidCard = "VALID001";
    public const string RevokedCard = "REVOKED1";

    public TestSiteData()
    {
        Directory.CreateDirectory(DataDirectory);
        Store = new SiteStore(DatabasePath);
        Identity = new SiteIdentity(Guid.NewGuid(), "Test", "key", [Door], [ValidCard, RevokedCard]);
    }

    public static Guid Door { get; } = Guid.NewGuid();

    public static Guid Group { get; } = Guid.NewGuid();

    public string DataDirectory { get; } = Path.Combine(Path.GetTempPath(), "accesshub-sim-tests", Guid.NewGuid().ToString("N"));

    public string DatabasePath => Path.Combine(DataDirectory, "site.db");

    public SiteStore Store { get; }

    public SiteIdentity Identity { get; }

    public static SimulatorMetrics Metrics() => new(new NoopMeterFactory());

    public void SeedSnapshot(long sequence = 4)
    {
        var group = new AccessListEntry { Kind = EntityKind.AccessGroup, EntityId = Group.ToString(), AccessGroup = new AccessGroupEntry { Name = "All" } };
        var access = new GroupDoorAccess { DoorId = Door.ToString() };
        access.Windows.AddRange(Enum.GetValues<DayOfWeek>().Select(d => new TimeWindow { DayOfWeek = (int)d, StartMinute = 0, EndMinute = 1440 }));
        group.AccessGroup.Doors.Add(access);
        Store.ReplaceSnapshot(
            "Europe/London",
            [
                new AccessListEntry { Kind = EntityKind.Door, EntityId = Door.ToString(), Door = new DoorEntry { Name = "Front" } },
                group,
                Card(ValidCard, revoked: false),
                Card(RevokedCard, revoked: true),
            ],
            sequence);
    }

    public static AccessListEntry Card(string number, bool revoked)
    {
        var entry = new AccessListEntry
        {
            Kind = EntityKind.Card,
            EntityId = Guid.NewGuid().ToString(),
            Card = new CardEntry { CardNumber = number, CardholderId = Guid.NewGuid().ToString(), IsRevoked = revoked, CardholderActive = true },
        };
        entry.Card.AccessGroupIds.Add(Group.ToString());
        return entry;
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (Directory.Exists(DataDirectory))
        {
            Directory.Delete(DataDirectory, recursive: true);
        }
    }

    private sealed class NoopMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);

        public void Dispose()
        {
        }
    }
}
