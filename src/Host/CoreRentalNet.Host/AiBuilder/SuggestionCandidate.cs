namespace CoreRentalNet.Host.AiBuilder;

/// <summary>One candidate a customer may act on: named, priced and totalled.</summary>
/// <remarks>
/// <para>
/// Everything here is the application's. The agent supplied the SKUs, the quantities and the purpose; the
/// names, the amounts and the total were resolved from the catalogue. So a candidate cannot carry a price the
/// catalogue does not charge.
/// </para>
/// <para>
/// <b>There is no rank and no label.</b> The application does not order the agent's options and does not call
/// them Budget, Balanced or Premium: the candidates are shown in the order the agent returned them, each one
/// checked and priced. Ordering and labelling the options was a decision this build drops.
/// </para>
/// </remarks>
/// <param name="MonthlyTotal">The whole candidate's monthly cost, summed from the catalogue.</param>
/// <param name="Rationale">Why this setup, in the model's own words.</param>
/// <param name="Lines">What it is made of.</param>
internal sealed record SuggestionCandidate(
    decimal MonthlyTotal,
    string Rationale,
    IReadOnlyList<SuggestionCandidateLine> Lines);
