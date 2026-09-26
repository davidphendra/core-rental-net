namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

/// <summary>One candidate a customer may act on: its lines as the agent stated them, and their total.</summary>
/// <remarks>
/// <para>
/// The names and the amounts are the agent's, taken from the catalogue tool's own answers; the total is the
/// application's, because the lines are added up here rather than by the model. What holds a candidate to the
/// catalogue is the workspace: applying one writes a command that the module refuses when the product, the
/// quantity or the slot cannot be honoured.
/// </para>
/// <para>
/// <b>There is no rank and no label.</b> The application does not order the agent's candidates and does not
/// call them Budget, Balanced or Premium: they are shown in the order the agent returned them.
/// </para>
/// </remarks>
public sealed record WorkspaceSuggestionCandidate(
    decimal MonthlyTotal,
    string Rationale,
    IReadOnlyList<WorkspaceSuggestionCandidateLine> Lines);
