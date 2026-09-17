using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Intent;
using AgentFoundry.WorkspaceSuggestions.Specifications;

namespace AgentFoundry.WorkspaceSuggestions.Workflows;

/// <summary>
/// One pass of the run: what the request was, what the verifier decided, what was composed, and what
/// the reviewer said about it.
/// </summary>
/// <remarks>
/// <para>
/// One type rather than three, and that is not a convenience. Two typed edges leading into the same
/// executor do not both arrive: the framework binds the executor's input from the first edge, so a
/// second edge carrying a different type is silently not delivered. One message flowing round the loop
/// is what makes the retry possible at all.
/// </para>
/// <para>
/// What each half means is therefore in the fields rather than in the type. The verifier sends one with
/// no specification - nothing has been composed yet - and the reviewer sends one with the composition
/// and its verdict. A reader can tell them apart by <see cref="Specification"/> being null, which is
/// exactly what it means.
/// </para>
/// </remarks>
public sealed record Round(
    SuggestionRequest Request,
    IntentVerdict Verdict,
    IReadOnlyList<Finding> Findings,
    int Attempt,
    Specification? Specification,
    IReadOnlyList<SuggestionOption> Options,
    bool Approved,
    bool Repeated)
{
    /// <summary>True when there is nothing left to try: approved, gone in circles, or out of attempts.</summary>
    public bool IsFinal(int attempts) => Approved || Repeated || Attempt >= attempts;
}
