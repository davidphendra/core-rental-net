using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreRentalNet.Modules.Discovery.Infrastructure.Configuration;

internal sealed class CatalogVectorEntityTypeConfiguration : IEntityTypeConfiguration<CatalogVector>
{
    public void Configure(EntityTypeBuilder<CatalogVector> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Discovery_CatalogVector");
        builder.HasKey(vector => vector.Sku);
        builder.Property(vector => vector.Sku).HasMaxLength(32).ValueGeneratedNever();

        // Converted to a blob rather than left to the provider, which would store a float[] as JSON text:
        // 2,048 bytes of binary per vector against roughly three times that as digits.
        builder.Property(vector => vector.Embedding)
            .HasConversion(values => VectorBlob.ToBytes(values), bytes => VectorBlob.ToFloats(bytes))
            .IsRequired();
    }
}
