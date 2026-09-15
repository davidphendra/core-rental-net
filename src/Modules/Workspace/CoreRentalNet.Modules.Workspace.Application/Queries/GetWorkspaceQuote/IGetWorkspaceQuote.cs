using CoreRentalNet.Modules.Workspace.Application.Queries.Views;

namespace CoreRentalNet.Modules.Workspace.Application.Queries.GetWorkspaceQuote;

/// <summary>The priced workspace behind a draft token.</summary>
public interface IGetWorkspaceQuote
{
    Task<WorkspaceQuote> HandleAsync(GetWorkspaceQuote query, CancellationToken cancellationToken = default);
}
