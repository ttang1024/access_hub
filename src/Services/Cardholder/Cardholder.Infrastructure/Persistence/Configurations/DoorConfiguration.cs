using AccessHub.Cardholder.Domain.Doors;
using AccessHub.Cardholder.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessHub.Cardholder.Infrastructure.Persistence.Configurations;

internal sealed class DoorConfiguration : IEntityTypeConfiguration<Door>
{
    public void Configure(EntityTypeBuilder<Door> builder)
    {
        builder.ToTable("doors");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasConversion(id => id.Value, v => new DoorId(v));
        builder.Property(d => d.SiteId).HasConversion(id => id.Value, v => new SiteId(v));
        builder.Property(d => d.Name).HasMaxLength(Door.MaxNameLength);
        builder.HasOne<Site>().WithMany().HasForeignKey(d => d.SiteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(d => d.SiteId);
        builder.HasXminRowVersion();
        builder.Ignore(d => d.DomainEvents);
    }
}
