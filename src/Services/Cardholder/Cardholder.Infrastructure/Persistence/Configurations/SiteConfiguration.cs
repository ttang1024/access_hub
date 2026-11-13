using AccessHub.Cardholder.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessHub.Cardholder.Infrastructure.Persistence.Configurations;

internal sealed class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder.ToTable("sites");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasConversion(id => id.Value, v => new SiteId(v));
        builder.Property(s => s.Name).HasMaxLength(Site.MaxNameLength);
        builder.Property(s => s.TimeZoneId).HasMaxLength(64);
        builder.Property(s => s.ApiKeyHash).HasMaxLength(32);
        builder.HasXminRowVersion();
        builder.Ignore(s => s.DomainEvents);
    }
}
