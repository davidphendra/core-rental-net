using CoreRentalNet.Modules.Discovery.Infrastructure;
using CoreRentalNet.Modules.Rentals.Infrastructure;
using CoreRentalNet.Modules.Workspace.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// Applies each module's migrations at startup, in development only.
/// </summary>
/// <remarks>
/// Development only because the database file is disposable there. Anywhere else the schema is a
/// deliberate step rather than a side effect of the application starting, which is a decision that
/// has not been made yet — and the reason a Production start currently finds no tables.
/// </remarks>
internal static class DatabaseStartup
{
    public static async Task ApplyMigrationsInDevelopmentAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        using var scope = app.Services.CreateScope();

        await scope.ServiceProvider.GetRequiredService<WorkspaceContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<RentalsContext>().Database.MigrateAsync();

        // The index's tables, so a fresh clone can run the ingestion tool and then a run without a manual
        // migration step first. The ROWS are not created here - that is the tool's deliberate job.
        await scope.ServiceProvider.GetRequiredService<DiscoveryContext>().Database.MigrateAsync();
    }
}
