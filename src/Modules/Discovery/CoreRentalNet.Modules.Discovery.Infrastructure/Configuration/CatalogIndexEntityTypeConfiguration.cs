using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreRentalNet.Modules.Discovery.Infrastructure.Configuration;

internal sealed class CatalogIndexEntityTypeConfiguration : IEntityTypeConfiguration<CatalogIndex>
{
    public void Configure(EntityTypeBuilder<CatalogIndex> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Discovery_Index");
        builder.HasKey(index => index.Name);
        builder.Property(index => index.Name).HasMaxLength(32).ValueGeneratedNever();
        builder.Property(index => index.ModelId).HasMaxLength(64).IsRequired();
        builder.Property(index => index.Width).IsRequired();
        builder.Property(index => index.Composition).HasMaxLength(200).IsRequired();
        builder.Property(index => index.CatalogueHash).HasMaxLength(64).IsRequired();
    }
}
