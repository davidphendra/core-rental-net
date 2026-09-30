namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Wire;

/// <summary>One product a catalogue search returned for one component of the workspace.</summary>
/// <remarks>
/// <b>It carries the description, and that is the point of the stage it feeds.</b> The name search orders by how
/// much a name resembles a term and the meaning search by one vector, so nothing before the reranker reads what a
/// product actually is — "adjustable lumbar support" is in the description of a chair whose name says "cute
/// office chair". Every value here is the tool's, taken from the recorded answer rather than from a model's report.
/// </remarks>
public sealed record RetrievedWorkspaceComponentProduct(
    WorkspaceSlot Slot,
    string Sku,
    string Name,
    string Description,
    decimal Amount);
