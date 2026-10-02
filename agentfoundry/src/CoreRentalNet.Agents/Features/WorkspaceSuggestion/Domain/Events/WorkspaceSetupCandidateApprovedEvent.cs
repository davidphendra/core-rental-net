using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Events;
/// <summary>One approved workspace setup, streamed the moment the reviewer accepted it.</summary>
/// <remarks>
/// Emitted only after review. A setup the reviewer has not accepted is not a result a caller may show, so an
/// event on this stream is always a setup the customer may act on.
/// </remarks>
public sealed record WorkspaceSetupCandidateApprovedEvent(
    string CustomerWorkflowIdentifier,
    WorkspaceSetupCandidate ApprovedWorkspaceSetup)
    : WorkspaceSuggestionStreamEvent(CustomerWorkflowIdentifier);
