namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;

/// <summary>A fragment of the agent's own text, exactly as it arrived, kept and never shown.</summary>
/// <remarks>
/// The run's evidence rather than its presentation: the whole streamed answer is what tells a bad prompt from a
/// bad catalogue after the fact, and it is written to the run record. Nothing here is rendered.
/// </remarks>
public sealed record WorkspaceSuggestionRawOutputEvent(string RawText) : WorkspaceSuggestionEvent;
