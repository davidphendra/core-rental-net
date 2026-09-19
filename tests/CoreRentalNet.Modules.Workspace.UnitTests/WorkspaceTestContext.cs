using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Workspace.Application.Services;
using CoreRentalNet.Modules.Workspace.Application.Queries.Views;
using CoreRentalNet.Modules.Workspace.Application.Rules;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

internal sealed class WorkspaceTestContext
{
    public const string RawToken = "test-raw-draft-token";

    public static IOpaqueTokenService Tokens { get; } = new OpaqueTokenService();

    public static IMoneyService Money { get; } = new MoneyService();

    public TestCatalog Catalog { get; } = new();

    public InMemoryWorkspaceRepository Repository { get; } = new();

    public ISlotRuleProvider SlotRules { get; } = new SlotRuleProvider();

    public IWorkspaceService Workspaces { get; } = new WorkspaceService(new SlotRuleProvider());

    public IWorkspaceCompositionService Compositions { get; } = new WorkspaceCompositionService(new SlotRuleProvider());

    public IWorkspaceQueryService Queries { get; } = new WorkspaceQueryService(new SlotRuleProvider());

    public string Token => RawToken;

    public IWorkspaceQuoteService Quotes => new WorkspaceQuoteService(Money, Catalog);

    public IWorkspaceViewService Views => new WorkspaceViewService(SlotRules, Quotes, Queries);

    public async Task<Domain.Workspace> DraftAsync()
    {
        var workspace = NewDraft();
        await Repository.AddAsync(workspace);
        return workspace;
    }

    /// <summary>Puts a product in a slot the way the page does, and keeps the version honest.</summary>
    public Task AssignAsync(Domain.Workspace workspace, SlotId slot, string sku)
    {
        Workspaces.Assign(workspace, slot, sku);

        return Task.CompletedTask;
    }

    public static Domain.Workspace NewDraft() => new()
    {
        Id = WorkspaceId.New(),
        DraftTokenHash = Tokens.HashOf(RawToken),
        State = DraftState.Draft,
        Version = 1,
        Assignments = [],
    };
}
