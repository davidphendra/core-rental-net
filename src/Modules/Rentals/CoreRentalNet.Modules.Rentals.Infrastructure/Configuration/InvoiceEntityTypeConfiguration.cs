using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CoreRentalNet.Modules.Rentals.Domain.Invoices;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Infrastructure.Configuration;

internal sealed class InvoiceEntityTypeConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Rentals_Invoice");
        builder.HasKey(invoice => invoice.Id);
        builder.Property(invoice => invoice.Id)
            .HasConversion(id => id.Value, value => InvoiceId.From(value))
            .ValueGeneratedNever();

        builder.Property(invoice => invoice.Number)
            .HasConversion(number => number.Value, value => InvoiceNumber.Parse(value))
            .HasMaxLength(17)
            .IsRequired();

        builder.HasIndex(invoice => invoice.Number).IsUnique();

        builder.Property(invoice => invoice.RentalId)
            .HasConversion(id => id.Value, value => RentalId.From(value))
            .IsRequired();

        builder.HasIndex(invoice => new { invoice.RentalId, invoice.PeriodIndex }).IsUnique();

        builder.Property(invoice => invoice.PeriodIndex).IsRequired();
        builder.Property(invoice => invoice.PeriodStart).IsRequired();
        builder.Property(invoice => invoice.PeriodEnd).IsRequired();
        builder.Property(invoice => invoice.Subtotal).HasConversion(MoneyConversion.Converter).HasPrecision(18, 2).IsRequired();
        builder.Property(invoice => invoice.TaxRate).HasPrecision(5, 4).IsRequired();
        builder.Property(invoice => invoice.TaxAmount).HasConversion(MoneyConversion.Converter).HasPrecision(18, 2).IsRequired();
        builder.Property(invoice => invoice.DeliveryFee).HasConversion(MoneyConversion.Converter).HasPrecision(18, 2).IsRequired();
        builder.Property(invoice => invoice.Total).HasConversion(MoneyConversion.Converter).HasPrecision(18, 2).IsRequired();
        builder.Property(invoice => invoice.Status).HasConversion<int>().IsRequired();
        builder.Property(invoice => invoice.IssuedOn).IsRequired();
        builder.Property(invoice => invoice.Version).IsConcurrencyToken().IsRequired();

        ConfigureLines(builder);
    }

    private static void ConfigureLines(EntityTypeBuilder<Invoice> builder)
    {
        builder.OwnsMany(invoice => invoice.Lines, lines =>
        {
            lines.ToTable("Rentals_InvoiceLine");
            lines.WithOwner().HasForeignKey("InvoiceId");
            lines.HasKey("InvoiceId", nameof(InvoiceLine.Sku));
            lines.Property(line => line.Sku).HasMaxLength(32).IsRequired();
            lines.Property(line => line.Name).HasMaxLength(120).IsRequired();
            lines.Property(line => line.Quantity).IsRequired();
            lines.Property(line => line.UnitMonthlyPrice).HasConversion(MoneyConversion.Converter).HasPrecision(18, 2).IsRequired();
            lines.Property(line => line.LineTotal).HasConversion(MoneyConversion.Converter).HasPrecision(18, 2).IsRequired();
        });
    }
}
