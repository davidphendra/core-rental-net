using CoreRentalNet.Modules.Rentals.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Modules.Rentals.Infrastructure;

/// <summary>
/// This module's database. It owns the <c>Rentals_</c> tables and nothing else (ADR-0002).
/// </summary>
public sealed class RentalsContext(DbContextOptions<RentalsContext> options) : DbContext(options)
{
    public DbSet<Domain.Rental> Rentals => Set<Domain.Rental>();

    public DbSet<Domain.Invoice> Invoices => Set<Domain.Invoice>();

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
