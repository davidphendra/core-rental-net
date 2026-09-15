using CoreRentalNet.Modules.Workspace.Application.Queries.Views;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Queries.Services;

/// <summary>Prices a draft from the catalog. The single place a workspace total is produced.</summary>
public interface IWorkspaceQuoteService
{
    WorkspaceQuote Quote(Domain.Workspace workspace);
}
