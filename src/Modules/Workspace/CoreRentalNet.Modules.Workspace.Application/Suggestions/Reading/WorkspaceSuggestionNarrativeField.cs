namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Reading;

/// <summary>One narrative field, complete, exactly as the model wrote it.</summary>
/// <remarks>
/// <b>Complete</b> is the whole point: the reader emits a field only once its JSON string has closed, so
/// hygiene runs on a finished value. A URL split across two streamed chunks cannot slip through, because
/// nothing is emitted while it is still half-arrived.
/// </remarks>
public sealed record WorkspaceSuggestionNarrativeField(
    WorkspaceSuggestionNarrativeFieldKind Kind,
    string Text);
