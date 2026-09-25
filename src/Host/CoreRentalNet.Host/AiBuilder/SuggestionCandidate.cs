namespace CoreRentalNet.Host.AiBuilder;

/// <summary>One candidate a customer may act on: its lines as the agent stated them, and their total.</summary>
/// <remarks>
/// <para>
/// The names and the amounts are the agent's, taken from the catalogue tool's own answers; the total is the
/// application's, because the lines are added up here rather than by the model. What holds a candidate to the
/// catalogue is the workspace: applying one writes a command that the module refuses when the product, the
/// quantity or the slot cannot be honoured.
/// </para>
/// <para>
/// <b>There is no rank and no label.</b> The application does not order the agent's options and does not call
/// them Budget, Balanced or Premium: the candidates are shown in the order the agent returned them, each one
/// checked and priced. Ordering and labelling the options was a decision this build drops.
/// </para>
/// </remarks>
/// <param name="MonthlyTotal">The whole candidate's monthly cost, summed from its lines here.</param>
/// <param name="Rationale">Why this setup, in the model's own words.</param>
/// <param name="Lines">What it is made of.</param>
internal sealed record SuggestionCandidate(
    decimal MonthlyTotal,
    string Rationale,
    IReadOnlyList<SuggestionCandidateLine> Lines);
