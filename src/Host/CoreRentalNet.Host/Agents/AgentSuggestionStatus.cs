namespace CoreRentalNet.Host.Agents;

/// <summary>Whether the agent proposed candidates or refused the request as not about a workspace.</summary>
/// <remarks>
/// A refusal is a <b>result</b>, not a failure: the application words it and reports no error. Serialized as
/// <c>suggested</c> / <c>notWorkspace</c> by the camel-case enum converter the application already uses.
/// </remarks>
internal enum AgentSuggestionStatus
{
    Suggested,
    NotWorkspace,
}
