using CoreRentalNet.Modules.Discovery.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Modules.Discovery.Infrastructure;

/// <summary>
/// This module's database. It owns the <c>Discovery_</c> tables and nothing else; another module's tables
/// are not reachable from here.
/// </summary>
/// <remarks>
/// <para>
/// The tables live here rather than beside the catalogue, and that is the whole reason this module exists.
/// The vectors are derived from <c>products.json</c> and the selection signal is written to at run time, so
/// neither is the catalogue: <c>CatalogReadOnlyTests</c> (CAT-13) asserts of that module that it exposes no
/// way to change a product and <b>has no persistence layer at all</b>. Owning these tables from there would
/// mean weakening a recorded decision to make room for a change — a test passing while its own claim is
/// false, which is worse than one that fails.
/// </para>
/// </remarks>
public sealed class DiscoveryContext(DbContextOptions<DiscoveryContext> options) : DbContext(options)
{
    /// <summary>One row per catalogue product: its embedding.</summary>
    public DbSet<CatalogVector> Vectors => Set<CatalogVector>();

    /// <summary>What the vectors were built from. One row.</summary>
    public DbSet<CatalogIndex> Indexes => Set<CatalogIndex>();

    /// <summary>One row per product that has ever been offered: the selection signal.</summary>
    public DbSet<Selection.CatalogSelection> Selections => Set<Selection.CatalogSelection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new CatalogVectorEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogIndexEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogSelectionEntityTypeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
