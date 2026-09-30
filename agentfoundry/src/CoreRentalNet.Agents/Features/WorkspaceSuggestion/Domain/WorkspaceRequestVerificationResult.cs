using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

/// <summary>Whether the customer's sentence is a workspace request at all, and why not when it is not.</summary>
/// <remarks>
/// The verdict is a result, not an error: a refused run ends as <see cref="WorkspaceSuggestionRunStatus.Rejected" />
/// and the application words the reason.
/// </remarks>
public sealed record WorkspaceRequestVerificationResult(
    [property: JsonPropertyName("isWorkspaceRequest")]
    bool IsWorkspaceRequest,
    [property: JsonPropertyName("refusalReason")]
    string? RefusalReason);
