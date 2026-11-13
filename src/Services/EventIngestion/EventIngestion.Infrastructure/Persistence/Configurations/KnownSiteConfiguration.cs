using AccessHub.EventIngestion.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessHub.EventIngestion.Infrastructure.Persistence.Configurations;

internal sealed class KnownSiteConfiguration : IEntityTypeConfiguration<KnownSite>
{
    public void Configure(EntityTypeBuilder<KnownSite> builder)
    {
        builder.ToTable("sites");
        builder.HasKey(s => s.SiteId);
        builder.Property(s => s.ApiKeyHash).HasMaxLength(32);
    }
}
