namespace CoreRentalNet.Host.AiBuilder;

/// <summary>That a customer chose one of a run's candidates, joined to the run by its id.</summary>
/// <remarks>
/// Separate from <see cref="AiRunRecord"/> because it is a different fact at a different time: a run ends
/// whether or not anybody acts on it, and most are never applied. Written when a candidate is chosen, so a
/// reader that wants "what did this run lead to" joins the two on <see cref="RunId"/>.
/// </remarks>
internal sealed record AiRunApplication(string RunId, decimal MonthlyTotal);
