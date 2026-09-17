namespace AgentFoundry.WorkspaceSuggestions.Prompts;

/// <summary>
/// A prompt could not be rendered, or its answer was not one.
/// </summary>
/// <remarks>
/// Deliberately not handled where it is thrown. A model that answers with nothing parseable has not
/// answered, and the two choices are to invent an answer or to fail - and inventing one about a
/// customer's request is the worse of the two, because a run that fails is visible and a run that
/// guessed is not.
/// </remarks>
public sealed class PromptAnswerException(string message) : Exception(message);
