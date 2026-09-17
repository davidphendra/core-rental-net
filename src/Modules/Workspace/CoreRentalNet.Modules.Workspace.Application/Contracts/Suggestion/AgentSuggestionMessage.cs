namespace CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

/// <summary>One message the agent sent: a stage it passed, or the result it ended with.</summary>
/// <remarks>
/// Discriminated by <see cref="Kind"/> because the wire contract is: the agent publishes a <c>kind</c>
/// of <c>stage</c> or <c>result</c>, and mirroring that here means the adapter does not have to invent a
/// shape the agent never agreed to. <see cref="Result"/> is null on a stage, which is what a stage is.
/// </remarks>
public sealed record AgentSuggestionMessage(
    string Kind,
    string? Stage,
    int Attempt,
    AgentSuggestion? Result)
{
    /// <summary>A stage transition, streamed while the run is happening.</summary>
    public static AgentSuggestionMessage StageEvent(string stage, int attempt)
        => new("stage", stage, attempt, Result: null);

    /// <summary>The run's answer, sent once, at the end.</summary>
    public static AgentSuggestionMessage Answer(AgentSuggestion result)
        => new("result", Stage: null, Attempt: 0, result);
}
