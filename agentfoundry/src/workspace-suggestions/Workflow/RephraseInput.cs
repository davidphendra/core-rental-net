using AgentFoundry.WorkspaceSuggestions.Contracts;

namespace AgentFoundry.WorkspaceSuggestions.Workflows;

/// <summary>
/// What the rephraser is asked to work from, on the first attempt and on every retry.
/// </summary>
/// <remarks>
/// One base rather than two shapes, so the rephraser has one entry: on the first attempt the message is
/// a <see cref="Verification"/> the verifier produced, and on a retry it is a <see cref="Review"/> the
/// reviewer produced with the findings against the composition it refused. The verifier itself runs
/// once, so anything arriving with a later attempt number did not come from it.
/// </remarks>
public abstract record RephraseInput(
    SuggestionRequest Request,
    IReadOnlyList<Finding> Findings,
    int Attempt);
