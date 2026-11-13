using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessHub.BuildingBlocks.Infrastructure.Auditing;

/// <summary>Maps <see cref="AuditEntry"/> to <c>audit_entries</c>.</summary>
public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(200);
        builder.Property(x => x.UserName).HasMaxLength(200);
        builder.Property(x => x.Action).HasMaxLength(100);
        builder.Property(x => x.EntityType).HasMaxLength(100);
        builder.Property(x => x.EntityId).HasMaxLength(100);
        builder.Property(x => x.Before).HasColumnType("jsonb");
        builder.Property(x => x.After).HasColumnType("jsonb");
        builder.Property(x => x.TraceId).HasMaxLength(64);
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => x.OccurredAt);
    }
}
