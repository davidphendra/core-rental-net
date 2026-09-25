using System.Text.Json;
using Microsoft.Extensions.AI;
using WorkspaceSuggestions.Contracts;
using WorkspaceSuggestions.Tools;

namespace WorkspaceSuggestions.Helpers;

/// <summary>The workspace request, with its MCP token taken out before the model can read it.</summary>
/// <remarks>
/// The token is a credential and the model's message is the prompt, so the two cannot be the same string: the
/// request is re-serialized without it, and what the model is given is that.
/// </remarks>
internal sealed class SuggestionRequestSplitter : IMcpToolRequest<SuggestionRequest>
{
    /// <inheritdoc />
    public (IReadOnlyList<ChatMessage> Messages, string? Token) Split(IEnumerable<ChatMessage> messages)
    {
        var kept = new List<ChatMessage>();
        string? token = null;

        foreach (var message in messages)
        {
            if (message.Role != ChatRole.User || string.IsNullOrWhiteSpace(message.Text))
            {
                kept.Add(message);
                continue;
            }

            var request = JsonSerializer.Deserialize<SuggestionRequest>(message.Text, ContractJson.Options);
            token ??= request?.McpAccessToken;

            kept.Add(new ChatMessage(message.Role, JsonSerializer.Serialize(
                request! with { McpAccessToken = null },
                ContractJson.Options)));
        }

        return (kept, token);
    }
}
