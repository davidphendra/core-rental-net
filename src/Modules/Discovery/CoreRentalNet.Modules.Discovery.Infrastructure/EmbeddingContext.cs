using CoreRentalNet.Modules.Discovery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Modules.Discovery.Infrastructure;

/// <summary>
/// The vector store's database. It owns the recipe table, and the vector table beside it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The recipe is the only entity.</b> It is an ordinary relational table, so EF creates it, seeds it
/// and describes it in the snapshot.
/// </para>
/// <para>
/// <b>The vector table is not in this model and never will be.</b> It is a <c>vec0</c> virtual table,
/// which EF cannot author: its <c>CREATE VIRTUAL TABLE</c> is written by hand in the migration that
/// creates it. Keeping it out of the model is deliberate rather than a gap — the snapshot must not
/// claim to describe a table EF is forbidden to touch.
/// </para>
/// <para>
/// The entity set is internal: the composition root creates and migrates the context, but no other
/// caller reaches a table through it.
/// </para>
/// </remarks>
public sealed class EmbeddingContext(DbContextOptions<EmbeddingContext> options) : DbContext(options)
{
    internal DbSet<ProductVectorRecipe> Recipe => Set<ProductVectorRecipe>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new ProductVectorRecipeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
