using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>The exact bytes sent to the agent, and a hash of them.</summary>
/// <remarks>
/// The hash is computed over the payload <b>actually sent</b> rather than over the catalogue file, so a change
/// to the projection itself is captured as well as a change to a product. It is what lets the run record say
/// what a suggestion was drawn from.
/// </remarks>
internal sealed record AgentFoundrySuggestionRequestPayload(string Json, string Hash)
{
    public static AgentFoundrySuggestionRequestPayload From(WorkspaceSuggestionRequestPayload suggestionRequestPayload)
    {
        ArgumentNullException.ThrowIfNull(suggestionRequestPayload);

        var json = JsonSerializer.Serialize(suggestionRequestPayload, MicrosoftFoundrySuggestionAgentJson.Options);

        // The bytes that are sent, hashed as they are: nothing in this payload is a per-run secret any more, so
        // there is no field to take out first and no second serialization to keep in step with the first.
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();

        return new AgentFoundrySuggestionRequestPayload(json, hash);
    }
}
