using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessHub.AccessControl.Infrastructure.Persistence.Configurations;

internal sealed class StaleGrantDetectionConfiguration : IEntityTypeConfiguration<StaleGrantDetection>
{
    public void Configure(EntityTypeBuilder<StaleGrantDetection> builder)
    {
        builder.ToTable("stale_grant_detections");
        builder.HasKey(d => d.SourceEventId);
        builder.Property(d => d.Reason).HasMaxLength(32);
    }
}
