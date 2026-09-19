namespace WorkspaceSuggestions.Prompts;

/// <summary>The instructions an agent is configured with, and the version a run is recorded against.</summary>
/// <remarks>
/// Two members rather than a string, because the run record has to name <b>which</b> prompt produced a
/// suggestion. A bare string would make a prompt change invisible in the data.
/// </remarks>
internal interface IInstructionSource
{
    /// <summary>The prompt text.</summary>
    string Text { get; }

    /// <summary>The version, as the run record names it — for example <c>suggestion-agent.v1</c>.</summary>
    string Version { get; }
}
