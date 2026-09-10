using CoreRentalNet.Modules.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreRentalNet.Modules.Rentals.Infrastructure.Configuration;

internal sealed class RentalEntityTypeConfiguration : IEntityTypeConfiguration<Domain.Rental>
{
    public void Configure(EntityTypeBuilder<Domain.Rental> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Rentals_Rental");
        builder.HasKey(rental => rental.Id);
        builder.Property(rental => rental.Id)
            .HasConversion(id => id.Value, value => RentalId.From(value))
            .ValueGeneratedNever();

        builder.Property(rental => rental.Number)
            .HasConversion(number => number.Value, value => RentalNumber.Parse(value))
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(rental => rental.Number).IsUnique();

        builder.Property(rental => rental.AccessTokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(rental => rental.AccessTokenHash).IsUnique();

        builder.Property(rental => rental.Status).HasConversion<int>().IsRequired();
        builder.Property(rental => rental.DeliveryAddress).HasMaxLength(200).IsRequired();
        builder.Property(rental => rental.DeliveryFee).HasConversion(MoneyConversion.Converter).HasPrecision(18, 2).IsRequired();
        builder.Property(rental => rental.AnchorDate).IsRequired();
        builder.Property(rental => rental.PlacedOn).IsRequired();

        builder.Property(rental => rental.Version).IsConcurrencyToken().IsRequired();

        builder.OwnsMany(rental => rental.Lines, lines =>
        {
            lines.ToTable("Rentals_RentalLine");
            lines.WithOwner().HasForeignKey("RentalId");
            lines.HasKey("RentalId", nameof(RentalLine.Sku));
            lines.Property(line => line.Sku).HasMaxLength(32).IsRequired();
            lines.Property(line => line.Name).HasMaxLength(120).IsRequired();
            lines.Property(line => line.Quantity).IsRequired();
            lines.Property(line => line.UnitMonthlyPrice).HasConversion(MoneyConversion.Converter).HasPrecision(18, 2).IsRequired();
            lines.Ignore(line => line.LineTotal);
        });

        builder.Metadata
            .FindNavigation(nameof(Domain.Rental.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
