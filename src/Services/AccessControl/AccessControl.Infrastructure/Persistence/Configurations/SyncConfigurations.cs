using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessHub.AccessControl.Infrastructure.Persistence.Configurations;

internal sealed class SiteEntryConfiguration : IEntityTypeConfiguration<SiteEntry>
{
    public void Configure(EntityTypeBuilder<SiteEntry> builder)
    {
        builder.ToTable("site_entries");
        builder.HasKey(e => new { e.SiteId, e.Kind, e.EntityId });
        builder.HasIndex(e => new { e.Kind, e.EntityId });
        builder.Property(e => e.Payload).HasColumnType("text");
    }
}

internal sealed class SiteChangeConfiguration : IEntityTypeConfiguration<SiteChange>
{
    public void Configure(EntityTypeBuilder<SiteChange> builder)
    {
        builder.ToTable("site_changes");
        builder.HasKey(c => new { c.SiteId, c.Sequence });
        builder.Property(c => c.Payload).HasColumnType("text");
        builder.HasIndex(c => c.RecordedAt);
    }
}

internal sealed class SiteSyncStatusConfiguration : IEntityTypeConfiguration<SiteSyncStatus>
{
    public void Configure(EntityTypeBuilder<SiteSyncStatus> builder)
    {
        builder.ToTable("site_sync_status");
        builder.HasKey(s => s.SiteId);
    }
}
