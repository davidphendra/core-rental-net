using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreRentalNet.Modules.Workspace.Infrastructure;

/// <summary>Lets <c>dotnet ef</c> build the model without a host. This path is never connected to.</summary>
public sealed class WorkspaceContextFactory : IDesignTimeDbContextFactory<WorkspaceContext>
{
    public WorkspaceContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WorkspaceContext>();
        WorkspacePersistence.Configure(options, new SqliteDatabaseSettings("design-time.db"));

        return new WorkspaceContext(options.Options);
    }
}
