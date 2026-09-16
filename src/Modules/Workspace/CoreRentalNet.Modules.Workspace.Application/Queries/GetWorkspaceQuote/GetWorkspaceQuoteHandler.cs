using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Workspace.Application.Services;
using CoreRentalNet.Modules.Workspace.Application.Queries.Views;
using CoreRentalNet.Modules.Workspace.Application.Support;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Queries.GetWorkspaceQuote;

/// <summary>Answers <see cref="IGetWorkspaceQuoteHandler"/> through the quote service.</summary>
public sealed class GetWorkspaceQuoteHandler(
    IWorkspaceRepository repository,
    IOpaqueTokenService tokens,
    IWorkspaceQuoteService quotes) : IGetWorkspaceQuoteHandler
{
    public async Task<WorkspaceQuote> HandleAsync(GetWorkspaceQuoteQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var workspace = await WorkspaceResolver.ResolveAsync(repository, tokens, query.DraftToken, cancellationToken).ConfigureAwait(false);

        return quotes.Quote(workspace);
    }
}
