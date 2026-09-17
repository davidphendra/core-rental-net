namespace CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

/// <summary>What the application hands back while it is asking: a stage, or the answer.</summary>
/// <remarks>
/// The same discriminator the agent uses, and for the same reason: the caller renders these in the order
/// they arrive, and a result that overtook the stage it followed would describe a run nobody watched.
/// The outcome is <see cref="WorkspaceSuggestion"/> rather than what the agent said, because everything
/// a caller sees has already been checked against the catalogue.
/// </remarks>
public sealed record SuggestionUpdate(
    string Kind,
    string? Stage,
    int Attempt,
    WorkspaceSuggestion? Outcome)
{
    /// <summary>One stage the run passed. The application renders copy for the id; an unknown id renders nothing.</summary>
    public static SuggestionUpdate StageEvent(string stage, int attempt)
        => new("stage", stage, attempt, Outcome: null);

    /// <summary>The answer, sent once, at the end.</summary>
    public static SuggestionUpdate Answer(WorkspaceSuggestion outcome)
        => new("result", Stage: null, Attempt: 0, outcome);
}
