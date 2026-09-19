namespace CoreRentalNet.Host.AiBuilder;

/// <summary>One candidate a customer may act on: named, priced, totalled and labelled.</summary>
/// <remarks>
/// Everything here is the application's. The agent supplied the SKUs, the quantities and the purpose; the
/// names, the amounts and the total were resolved from the catalogue, and the label was assigned by rank. So
/// a candidate cannot carry a price the catalogue does not charge, and two candidates cannot claim the same
/// position in the range.
/// </remarks>
/// <param name="Label">Where this candidate sits in the range: Budget, Balanced or Premium.</param>
/// <param name="MonthlyTotal">The whole candidate's monthly cost, summed from the catalogue.</param>
/// <param name="Rationale">Why this setup, in the model's own words.</param>
/// <param name="Lines">What it is made of.</param>
internal sealed record SuggestionCandidate(
    string Label,
    decimal MonthlyTotal,
    string Rationale,
    IReadOnlyList<SuggestionCandidateLine> Lines);
