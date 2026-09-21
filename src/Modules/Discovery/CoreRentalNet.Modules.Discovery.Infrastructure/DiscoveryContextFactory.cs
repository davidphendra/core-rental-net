using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreRentalNet.Modules.Discovery.Infrastructure;

/// <summary>Lets <c>dotnet ef</c> build the model without a host. This path is never connected to.</summary>
public sealed class DiscoveryContextFactory : IDesignTimeDbContextFactory<DiscoveryContext>
{
    public DiscoveryContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DiscoveryContext>();
        DiscoveryPersistence.Configure(options, new SqliteDatabaseSettings("design-time.db"));

        return new DiscoveryContext(options.Options);
    }
}
