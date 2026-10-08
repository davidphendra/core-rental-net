using CoreRentalNet.Modules.Rentals.Infrastructure;
using CoreRentalNet.Modules.Workspace.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Host.Extentions;

/// <summary>
/// Applies each module's migrations at startup, wherever the application runs.
/// </summary>
/// <remarks>
/// This used to run in development only, on the argument that a deployed schema is a deliberate step
/// rather than a side effect of the process starting. The argument left every other environment with a
/// database file and no tables: the file is created when the settings are first read, so a start
/// outside development reached its first query with nothing to query and failed on
/// <c>no such table: Rentals_Rental</c>. The application must never scale out (README), so the one
/// instance that starts is the only writer, and applying the migrations here is safe.
/// </remarks>
internal static class DatabaseStartupExtentions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.Services.CreateScope();

        await scope.ServiceProvider.GetRequiredService<WorkspaceContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<RentalsContext>().Database.MigrateAsync();
    }
}
