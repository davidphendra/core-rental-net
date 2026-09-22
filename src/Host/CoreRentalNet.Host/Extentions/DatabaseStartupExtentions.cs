using CoreRentalNet.Modules.Rentals.Infrastructure;
using CoreRentalNet.Modules.Workspace.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Host.Extentions;

/// <summary>
/// Applies each module's migrations at startup, in development only.
/// </summary>
/// <remarks>
/// Development only because the database file is disposable there. Anywhere else the schema is a
/// deliberate step rather than a side effect of the application starting, which is a decision that
/// has not been made yet — and the reason a Production start currently finds no tables.
/// </remarks>
internal static class DatabaseStartupExtentions
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
    }
}
