namespace CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.StartDraft;

/// <summary>
/// Ensures a draft exists for a token.
/// </summary>
/// <remarks>
/// Creating the draft is a write, so it lives in a command rather than inside the read query:
/// the query stays a query. Running it twice for the same token is harmless and returns the
/// same draft (matrix DR-07).
/// </remarks>
public sealed record StartDraftCommand(string DraftToken);
