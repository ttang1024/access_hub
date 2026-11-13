using System.Text.Json;
using AccessHub.AccessControl.Domain.ReadModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessHub.AccessControl.Infrastructure.Persistence.Configurations;

// Read-model tables have no foreign keys between them: events arrive in any order.
internal sealed class SiteStateConfiguration : IEntityTypeConfiguration<SiteState>
{
    public void Configure(EntityTypeBuilder<SiteState> builder)
    {
        builder.ToTable("sites");
        builder.HasKey(s => s.SiteId);
        builder.Property(s => s.Name).HasMaxLength(200);
        builder.Property(s => s.TimeZoneId).HasMaxLength(64);
        builder.Property(s => s.ApiKeyHash).HasMaxLength(32);
    }
}

internal sealed class DoorStateConfiguration : IEntityTypeConfiguration<DoorState>
{
    public void Configure(EntityTypeBuilder<DoorState> builder)
    {
        builder.ToTable("doors");
        builder.HasKey(d => d.DoorId);
        builder.Property(d => d.Name).HasMaxLength(200);
        builder.HasIndex(d => d.SiteId);
    }
}

internal sealed class AccessGroupStateConfiguration : IEntityTypeConfiguration<AccessGroupState>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<AccessGroupState> builder)
    {
        builder.ToTable("access_groups");
        builder.HasKey(g => g.AccessGroupId);
        builder.Property(g => g.Name).HasMaxLength(200);
        builder.Ignore(g => g.SiteIds);
        builder.Property(g => g.Doors)
            .HasConversion(
                d => JsonSerializer.Serialize(d, Json),
                s => JsonSerializer.Deserialize<List<GroupDoorState>>(s, Json) ?? new List<GroupDoorState>(),
                new ValueComparer<IReadOnlyList<GroupDoorState>>(
                    (a, b) => JsonSerializer.Serialize(a, Json) == JsonSerializer.Serialize(b, Json),
                    v => JsonSerializer.Serialize(v, Json).GetHashCode(StringComparison.Ordinal),
                    v => v.ToList()))
            .HasColumnType("jsonb");
    }
}

internal sealed class CardholderStateConfiguration : IEntityTypeConfiguration<CardholderState>
{
    public void Configure(EntityTypeBuilder<CardholderState> builder)
    {
        builder.ToTable("cardholders");
        builder.HasKey(c => c.CardholderId);
        builder.Property(c => c.AccessGroupIds)
            .HasConversion(
                v => v.ToArray(),
                v => v.ToList(),
                new ValueComparer<IReadOnlyList<Guid>>((a, b) => a!.SequenceEqual(b!), v => v.Aggregate(0, (h, g) => HashCode.Combine(h, g)), v => v.ToList()))
            .HasColumnType("uuid[]");
        builder.HasIndex(c => c.AccessGroupIds).HasMethod("gin");
    }
}

internal sealed class CardStateConfiguration : IEntityTypeConfiguration<CardState>
{
    public void Configure(EntityTypeBuilder<CardState> builder)
    {
        builder.ToTable("cards");
        builder.HasKey(c => c.CardId);
        builder.Property(c => c.Number).HasMaxLength(32);
        builder.Ignore(c => c.IsRevoked);
        builder.HasIndex(c => c.CardholderId);
        builder.HasIndex(c => c.Number);
    }
}
