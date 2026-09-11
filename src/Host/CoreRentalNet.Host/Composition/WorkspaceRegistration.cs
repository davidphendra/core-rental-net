using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.Modules.Workspace.Application.Commands;
using CoreRentalNet.Modules.Workspace.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Queries;
using CoreRentalNet.Modules.Workspace.Domain;
using CoreRentalNet.Modules.Workspace.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Host.Composition;

/// <summary>The draft: the slots, the quantities and the delivery address.</summary>
internal static class WorkspaceRegistration
{
    public static void AddWorkspace(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddDbContext<WorkspaceContext>((provider, options) =>
        {
            WorkspacePersistence.Configure(options, provider.GetRequiredService<SqliteDatabaseSettings>());
            options.AddInterceptors(provider.GetRequiredService<SqlitePragmaInterceptor>());
        });

        builder.Services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        builder.Services.AddScoped<IDefineWorkspaceComposition, DefineWorkspaceComposition>();

        builder.Services.AddScoped<GetWorkspaceHandler>();
        builder.Services.AddScoped<GetWorkspaceQuoteHandler>();
        builder.Services.AddScoped<StartDraftHandler>();
        builder.Services.AddScoped<AssignProductHandler>();
        builder.Services.AddScoped<RemoveAssignmentHandler>();
        builder.Services.AddScoped<ChangeQuantityHandler>();
        builder.Services.AddScoped<SetDeliveryAddressHandler>();
    }
}
