using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Workspace;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Queries;

public sealed record GetWorkspaceQuote(string DraftToken);

public sealed class GetWorkspaceQuoteHandler(IWorkspaceRepository repository, IDefineProductPrices prices)
{
    public async Task<WorkspaceQuote> HandleAsync(GetWorkspaceQuote query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var workspace = await WorkspaceResolver.ResolveAsync(repository, query.DraftToken, cancellationToken).ConfigureAwait(false);

        return WorkspaceQuoter.Quote(workspace, prices);
    }
}
