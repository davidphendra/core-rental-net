using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreRentalNet.Modules.Workspace.Infrastructure;

/// <summary>
/// Lets <c>dotnet ef</c> build the model without a host and without touching a real database.
/// The path here is never used to connect.
/// </summary>
public sealed class WorkspaceContextFactory : IDesignTimeDbContextFactory<WorkspaceContext>
{
    public WorkspaceContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WorkspaceContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;

        return new WorkspaceContext(options);
    }
}
