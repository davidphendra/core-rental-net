using CoreRentalNet.Modules.Rentals.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using CoreRentalNet.Modules.Rentals.Domain.Invoices;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Infrastructure;

/// <summary>
/// This module's database. It owns the <c>Rentals_</c> tables and nothing else.
/// </summary>
public sealed class RentalsContext(DbContextOptions<RentalsContext> options) : DbContext(options)
{
    public DbSet<Rental> Rentals => Set<Rental>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<NumberSequenceEntry> NumberSequences => Set<NumberSequenceEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new RentalEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new InvoiceEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new NumberSequenceEntityTypeConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
