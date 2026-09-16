using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.Modules.Workspace.Application.Commands.AssignProduct;
using CoreRentalNet.Modules.Workspace.Application.Commands.ChangeQuantity;
using CoreRentalNet.Modules.Workspace.Application.Commands.RemoveAssignment;
using CoreRentalNet.Modules.Workspace.Application.Commands.SetDeliveryAddress;
using CoreRentalNet.Modules.Workspace.Application.Commands.StartDraft;
using CoreRentalNet.Modules.Workspace.Application.Contracts.Composition;
using CoreRentalNet.Modules.Workspace.Application.Queries.GetWorkspace;
using CoreRentalNet.Modules.Workspace.Application.Services;
using CoreRentalNet.Modules.Workspace.Application.Rules;
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
        // hold everything that used to live on the aggregate. The capacities are read here so that
        // retuning a slot is a configuration change, not a rebuild.
        builder.Services.AddSingleton(ReadSlotSettings(builder.Configuration));
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

    /// <summary>
    /// The slot capacities, from configuration. Every key falls back to its shipped default, so a
    /// deployment overrides only the slots it wants to change.
    /// </summary>
    private static WorkspaceSlotSettings ReadSlotSettings(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new WorkspaceSlotSettings(
            Desk: configuration.GetValue("Workspace:SlotCapacity:Desk", 1),
            Chair: configuration.GetValue("Workspace:SlotCapacity:Chair", 1),
            Monitor: configuration.GetValue("Workspace:SlotCapacity:Monitor", 3),
            Lamp: configuration.GetValue("Workspace:SlotCapacity:Lamp", 1),
            Plant: configuration.GetValue("Workspace:SlotCapacity:Plant", 1),
            CoffeeStation: configuration.GetValue("Workspace:SlotCapacity:CoffeeStation", 1),
            RelaxZone: configuration.GetValue("Workspace:SlotCapacity:RelaxZone", 1));
    }
}
