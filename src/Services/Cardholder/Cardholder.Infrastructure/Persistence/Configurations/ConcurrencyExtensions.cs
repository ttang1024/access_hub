using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessHub.Cardholder.Infrastructure.Persistence.Configurations;

internal static class ConcurrencyExtensions
{
    /// <summary>Optimistic concurrency on Postgres' xmin system column, kept out of the domain model as a shadow property.</summary>
    public static void HasXminRowVersion<T>(this EntityTypeBuilder<T> builder)
        where T : class =>
        builder.Property<uint>("RowVersion").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();
}
