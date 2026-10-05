namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>The kind of event the agent streamed, as the contract's schema names it.</summary>
/// <remarks>
/// A closed set, so it is an enum rather than a string the reader compares against literals. The wire name is the
/// camel-cased member, which <see cref="WorkspaceSuggestionAgentJson.Options"/> already applies — so the schema's
/// spelling and this set are one thing rather than two that can drift.
/// </remarks>
internal enum WorkspaceSuggestionStreamEventType
{
    StageStarted,
    StageCompleted,
    Candidate,
    Retry,
    Completed,
}
