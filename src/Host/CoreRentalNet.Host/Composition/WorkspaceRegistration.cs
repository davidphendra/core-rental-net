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

        // By the operation each one performs, not by its own type. The session is their only consumer,
        // and it names the operation too; how a draft is started or a quantity changed is the
        // application's business.
        builder.Services.AddScoped<IGetWorkspace, GetWorkspaceHandler>();
        builder.Services.AddScoped<IStartDraft, StartDraftHandler>();
        builder.Services.AddScoped<IAssignProduct, AssignProductHandler>();
        builder.Services.AddScoped<IRemoveAssignment, RemoveAssignmentHandler>();
        builder.Services.AddScoped<IChangeQuantity, ChangeQuantityHandler>();
        builder.Services.AddScoped<ISetDeliveryAddress, SetDeliveryAddressHandler>();
    }
}
