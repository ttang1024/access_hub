using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessHub.Alarm.Infrastructure.Persistence.Configurations;

internal sealed class RecentDenialConfiguration : IEntityTypeConfiguration<RecentDenial>
{
    public void Configure(EntityTypeBuilder<RecentDenial> builder)
    {
        builder.ToTable("recent_denials");
        builder.HasKey(d => d.SourceEventId);
        builder.Property(d => d.CardNumber).HasMaxLength(32);
        builder.HasIndex(d => new { d.CardNumber, d.OccurredAt });
    }
}
