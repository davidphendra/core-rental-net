using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Specifications;

namespace AgentFoundry.WorkspaceSuggestions.Workflows;

/// <summary>
/// What the reviewer decides, and what it objected to.
/// </summary>
/// <remarks>
/// It carries the findings rather than a corrected specification: the reviewer judges, and the
/// rephraser writes. Two authors of one specification is how the two come to disagree about what a
/// slot is called, so this message either approves or says what is wrong.
/// </remarks>
public sealed record Review(
    SuggestionRequest Request,
    Specification Specification,
    IReadOnlyList<SuggestionOption> Options,
    IReadOnlyList<Finding> Findings,
    bool Approved,
    bool Repeated,
    int Attempt)
    : RephraseInput(Request, Findings, Attempt)
{
    /// <summary>True when there is nothing left to try: the attempts are spent, or the retry stopped moving.</summary>
    public bool IsFinal(int attempts) => Approved || Repeated || Attempt >= attempts;
}
