using Microsoft.Extensions.AI;

namespace WorkspaceSuggestions.Tools;

/// <summary>Reads the call's MCP token out of the request the agent was handed.</summary>
/// <remarks>
/// A port, so the decorator that holds the token is not tied to one request contract: it knows how to obtain a
/// token-carrying request and how to hand the model a version without it, and nothing about what that request
/// means.
/// </remarks>
internal interface IMcpToolRequest<TRequest>
{
    /// <summary>The messages the model may see, and the token they carried.</summary>
    (IReadOnlyList<ChatMessage> Messages, string? Token) Split(IEnumerable<ChatMessage> messages);
}
