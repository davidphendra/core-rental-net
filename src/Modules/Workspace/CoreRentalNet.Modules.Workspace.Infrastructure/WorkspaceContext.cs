using CoreRentalNet.Modules.Workspace.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Modules.Workspace.Infrastructure;

/// <summary>
/// This module's database. It owns the <c>Workspace_</c> tables and nothing else; another
/// module's tables are not reachable from here.
/// </summary>
public sealed class WorkspaceContext(DbContextOptions<WorkspaceContext> options) : DbContext(options)
{
    public DbSet<Domain.Workspace> Drafts => Set<Domain.Workspace>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new DraftEntityTypeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
