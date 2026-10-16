using CoreRentalNet.Modules.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Modules.Catalog.Infrastructure;

/// <summary>
/// The catalogue's database. It owns the <c>Catalog_</c> tables and nothing else.
/// </summary>
/// <remarks>
/// The entity set is internal: the composition root creates and migrates the context, but no other
/// caller reaches a table through it. What callers read is the published port, not this.
/// </remarks>
public sealed class CatalogContext(DbContextOptions<CatalogContext> options) : DbContext(options)
{
    internal DbSet<CatalogProduct> Products => Set<CatalogProduct>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new CatalogProductConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
