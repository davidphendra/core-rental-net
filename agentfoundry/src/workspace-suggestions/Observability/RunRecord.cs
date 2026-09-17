namespace AgentFoundry.WorkspaceSuggestions.Observability;

/// <summary>
/// What one run did, in the numbers later decisions are checked against.
/// </summary>
/// <remarks>
/// <para>
/// Every field here is one that a decision in this epic rests on and that nothing else would reveal:
/// the miss rate is the only thing that shows the intent table has stopped being useful, the exhausted
/// rate is the only signal that would justify a stronger reviewer, and the attempt count is what makes
/// a loop that thrashed visible rather than merely slow.
/// </para>
/// <para>
/// Not the customer's words. A request is a person's own text and it has no place in a log; the request
/// id is what ties a line to a run, and the length is enough to tell a one-word request from a
/// paragraph when that is what a miss rate needs to be read against.
/// </para>
/// </remarks>
public sealed record RunRecord(
    string RequestId,
    string Outcome,
    int Attempts,
    int QueryLength,
    bool SlotsInferred,
    int Findings,
    int CatalogueReads);
