using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreRentalNet.Modules.Rentals.Infrastructure;

/// <summary>Lets <c>dotnet ef</c> build the model without a host. This path is never connected to.</summary>
public sealed class RentalsContextFactory : IDesignTimeDbContextFactory<RentalsContext>
{
    public RentalsContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RentalsContext>();
        RentalsPersistence.Configure(options, new SqliteDatabaseSettings("design-time.db"));

        return new RentalsContext(options.Options);
    }
}
