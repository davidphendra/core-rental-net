using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

internal sealed class WorkspaceTestContext
{
    public const string RawToken = "test-raw-draft-token";

    public TestCatalog Catalog { get; } = new();

    public InMemoryWorkspaceRepository Repository { get; } = new();

    public string Token => RawToken;

    public async Task<Domain.Workspace> DraftAsync()
    {
        var workspace = Domain.Workspace.CreateNew(WorkspaceId.New(), DraftToken.FromRawToken(RawToken).Hash);
        await Repository.AddAsync(workspace);
        return workspace;
    }
}
