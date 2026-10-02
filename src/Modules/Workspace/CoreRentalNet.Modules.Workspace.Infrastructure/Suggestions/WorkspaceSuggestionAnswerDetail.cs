using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>Everything the streamed answer carried: the answer, the stages it ran, and the retries it made.</summary>
/// <remarks>
/// The stages and the retries are progress rather than result, so they are kept beside the answer and not inside
/// it: the answer is what the run produced, and this is how it got there.
/// </remarks>
internal sealed record WorkspaceSuggestionAnswerDetail(
    WorkspaceSuggestionAnswer Answer,
    IReadOnlyList<string> ProcessingStages,
    IReadOnlyList<MicrosoftFoundrySuggestionRetryAttempt> RetryAttempts);
