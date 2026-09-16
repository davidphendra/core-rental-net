using CoreRentalNet.Modules.Workspace.Application.Queries.Views;

namespace CoreRentalNet.Modules.Workspace.Application.Queries.GetWorkspaceQuote;

/// <summary>The priced workspace behind a draft token.</summary>
public interface IGetWorkspaceQuoteHandler
{
    Task<WorkspaceQuote> HandleAsync(GetWorkspaceQuoteQuery query, CancellationToken cancellationToken = default);
}
