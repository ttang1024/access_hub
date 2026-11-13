using AccessHub.EventIngestion.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessHub.EventIngestion.Infrastructure.Persistence.Configurations;

internal sealed class DoorEventConfiguration : IEntityTypeConfiguration<DoorEvent>
{
    public void Configure(EntityTypeBuilder<DoorEvent> builder)
    {
        builder.ToTable("door_events");

        // The unique (site, event) key is what makes replayed batches safe (ON CONFLICT DO NOTHING).
        builder.HasKey(e => new { e.SiteId, e.EventId });
        builder.Property(e => e.CardNumber).HasMaxLength(32);
        builder.Property(e => e.DenyReason).HasMaxLength(32);
        builder.Ignore(e => e.IsDecision);
        builder.HasIndex(e => new { e.SiteId, e.OccurredAt });
        builder.HasIndex(e => e.RecordedAt);
    }
}
