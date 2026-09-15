namespace CoreRentalNet.Modules.Workspace.Application.Queries.GetWorkspace;

/// <summary>Reads the caller's draft, by the raw token in its cookie.</summary>
public sealed record GetWorkspace(string DraftToken);
