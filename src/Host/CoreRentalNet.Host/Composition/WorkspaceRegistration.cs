using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.AssignProduct;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.ChangeQuantity;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.RemoveAssignment;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.SetDeliveryAddress;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.StartDraft;
using CoreRentalNet.Modules.Workspace.Application.Contracts.Composition;
using CoreRentalNet.Modules.Workspace.Application.Queries.GetWorkspace;
using CoreRentalNet.Modules.Workspace.Application.Queries.Services;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Rules;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Services;
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

        // The workspace's rules and projections, in services. The record holds data; these
        // hold everything that used to live on the aggregate.
        builder.Services.AddScoped<ISlotRuleProvider, SlotRuleProvider>();
        builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
        builder.Services.AddScoped<IWorkspaceQueryService, WorkspaceQueryService>();
        builder.Services.AddScoped<IWorkspaceQuoteService, WorkspaceQuoteService>();
        builder.Services.AddScoped<IWorkspaceViewService, WorkspaceViewService>();

        // By the operation each one performs, not by its own type. The session is their only consumer,
        // and it names the operation too; how a draft is started or a quantity changed is the
        // application's business.
        builder.Services.AddScoped<IGetWorkspaceHandler, GetWorkspaceHandler>();
        builder.Services.AddScoped<IStartDraftHandler, StartDraftHandler>();
        builder.Services.AddScoped<IAssignProductHandler, AssignProductHandler>();
        builder.Services.AddScoped<IRemoveAssignmentHandler, RemoveAssignmentHandler>();
        builder.Services.AddScoped<IChangeQuantityHandler, ChangeQuantityHandler>();
        builder.Services.AddScoped<ISetDeliveryAddressHandler, SetDeliveryAddressHandler>();
    }
}
