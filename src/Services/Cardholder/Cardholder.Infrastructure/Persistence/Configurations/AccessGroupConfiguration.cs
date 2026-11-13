using System.Text.Json;
using AccessHub.Cardholder.Domain.AccessGroups;
using AccessHub.Cardholder.Domain.Doors;
using AccessHub.Cardholder.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessHub.Cardholder.Infrastructure.Persistence.Configurations;

internal sealed class AccessGroupConfiguration : IEntityTypeConfiguration<AccessGroup>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<AccessGroup> builder)
    {
        builder.ToTable("access_groups");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasConversion(id => id.Value, v => new AccessGroupId(v));
        builder.Property(g => g.Name).HasMaxLength(AccessGroup.MaxNameLength);

        // Doors and schedules are values inside the aggregate; they are stored as one jsonb document.
        builder.Property(g => g.Doors)
            .HasField("_doors")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                doors => Serialize(doors),
                json => Deserialize(json),
                new ValueComparer<IReadOnlyList<DoorAccess>>(
                    (a, b) => a!.SequenceEqual(b!),
                    v => v.Aggregate(0, (h, d) => HashCode.Combine(h, d)),
                    v => v.ToList()))
            .HasColumnType("jsonb");
        builder.HasXminRowVersion();
        builder.Ignore(g => g.DomainEvents);
    }

    private static string Serialize(IReadOnlyList<DoorAccess> doors) =>
        JsonSerializer.Serialize(
            doors.Select(d => new DoorAccessRecord(d.DoorId.Value, d.SiteId.Value, d.Schedule.Windows.Select(w => new WindowRecord(w.Day, w.StartMinute, w.EndMinute)).ToList())).ToList(),
            Json);

    // Stored data was validated when written, so rebuilding the value objects cannot fail.
    private static List<DoorAccess> Deserialize(string json) =>
        (JsonSerializer.Deserialize<List<DoorAccessRecord>>(json, Json) ?? [])
            .Select(r => new DoorAccess(
                new DoorId(r.DoorId),
                new SiteId(r.SiteId),
                Schedule.Create(r.Windows.Select(w => TimeWindow.Create(w.Day, w.Start, w.End).Value).ToList()).Value))
            .ToList();

    private sealed record DoorAccessRecord(Guid DoorId, Guid SiteId, List<WindowRecord> Windows);

    private sealed record WindowRecord(DayOfWeek Day, int Start, int End);
}
