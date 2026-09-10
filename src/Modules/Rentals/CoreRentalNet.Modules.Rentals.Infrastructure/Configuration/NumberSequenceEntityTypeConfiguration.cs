using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreRentalNet.Modules.Rentals.Infrastructure.Configuration;

internal sealed class NumberSequenceEntityTypeConfiguration : IEntityTypeConfiguration<NumberSequenceEntry>
{
    public void Configure(EntityTypeBuilder<NumberSequenceEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Rentals_NumberSequence");
        builder.HasKey(entry => new { entry.Kind, entry.Year });
        builder.Property(entry => entry.Kind).ValueGeneratedNever();
        builder.Property(entry => entry.Year).ValueGeneratedNever();
        builder.Property(entry => entry.Value).IsRequired();
    }
}
