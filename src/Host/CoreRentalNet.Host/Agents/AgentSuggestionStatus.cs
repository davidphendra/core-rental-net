namespace CoreRentalNet.Host.Agents;

/// <summary>Whether the agent proposed candidates or refused the request as not about a workspace.</summary>
/// <remarks>
/// A refusal is a <b>result</b>, not a failure: the application words it and reports no error. Serialized as
/// <c>suggested</c> / <c>notWorkspace</c> / <c>catalogueUnavailable</c> by the camel-case enum converter the
/// application already uses. <see cref="CatalogueUnavailable"/> is the run that had no catalogue to search -
/// the customer was offered no tool, or one failed - and composed nothing from memory.
/// </remarks>
internal enum AgentSuggestionStatus
{
    Suggested,
    NotWorkspace,
    CatalogueUnavailable,
}
