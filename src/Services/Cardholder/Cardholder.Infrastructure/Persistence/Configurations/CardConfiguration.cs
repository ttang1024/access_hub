using AccessHub.Cardholder.Domain.Cardholders;
using AccessHub.Cardholder.Domain.Cards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CardholderAggregate = AccessHub.Cardholder.Domain.Cardholders.Cardholder;

namespace AccessHub.Cardholder.Infrastructure.Persistence.Configurations;

internal sealed class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("cards");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasConversion(id => id.Value, v => new CardId(v));
        builder.Property(c => c.CardholderId).HasConversion(id => id.Value, v => new CardholderId(v));
        builder.Property(c => c.Number).HasMaxLength(CardNumber.MaxLength);
        builder.HasIndex(c => c.Number).IsUnique();
        builder.HasIndex(c => c.CardholderId);
        builder.HasOne<CardholderAggregate>().WithMany().HasForeignKey(c => c.CardholderId).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(c => c.IsRevoked);
        builder.HasXminRowVersion();
        builder.Ignore(c => c.DomainEvents);
    }
}
