using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
/// <summary>One reason a reviewed setup set was not acceptable, as an instruction to the next attempt.</summary>
public sealed record WorkspaceSetupReviewIssue(
    [property: JsonPropertyName("issueCode")]
    string IssueCode,
    [property: JsonPropertyName("requiredCorrectionDescription")]
    string RequiredCorrectionDescription);
