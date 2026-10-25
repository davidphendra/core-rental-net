using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Events;
/// <summary>One approved workspace setup, streamed the moment the reviewer accepted it.</summary>
/// <remarks>
/// Emitted after the composer materialises the reranker result. Review is an observer, not a gate, so an
/// event on this stream is always a setup the customer may act on.
/// </remarks>
public sealed record WorkspaceSetupCandidateApprovedEvent(
    string CustomerWorkflowIdentifier,
    WorkspaceSetupCandidate ApprovedWorkspaceSetup)
    : WorkspaceSuggestionStreamEvent(CustomerWorkflowIdentifier);
