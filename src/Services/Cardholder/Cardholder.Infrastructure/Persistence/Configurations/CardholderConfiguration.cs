using AccessHub.Cardholder.Domain.AccessGroups;
using AccessHub.Cardholder.Domain.Cardholders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CardholderAggregate = AccessHub.Cardholder.Domain.Cardholders.Cardholder;

namespace AccessHub.Cardholder.Infrastructure.Persistence.Configurations;

internal sealed class CardholderConfiguration : IEntityTypeConfiguration<CardholderAggregate>
{
    public void Configure(EntityTypeBuilder<CardholderAggregate> builder)
    {
        builder.ToTable("cardholders");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasConversion(id => id.Value, v => new CardholderId(v));
        builder.Property(c => c.FirstName).HasMaxLength(CardholderAggregate.MaxNameLength);
        builder.Property(c => c.LastName).HasMaxLength(CardholderAggregate.MaxNameLength);
        builder.Property(c => c.AccessGroupIds)
            .HasField("_accessGroupIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                ids => ids.Select(i => i.Value).ToArray(),
                values => values.Select(v => new AccessGroupId(v)).ToList(),
                new ValueComparer<IReadOnlyList<AccessGroupId>>(
                    (a, b) => a!.SequenceEqual(b!),
                    v => v.Aggregate(0, (h, i) => HashCode.Combine(h, i)),
                    v => v.ToList()))
            .HasColumnType("uuid[]");
        builder.HasIndex(c => c.LastName);
        builder.HasXminRowVersion();
        builder.Ignore(c => c.DomainEvents);
    }
}
