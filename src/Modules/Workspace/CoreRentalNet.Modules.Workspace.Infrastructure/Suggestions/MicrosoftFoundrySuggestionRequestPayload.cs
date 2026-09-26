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
internal sealed record MicrosoftFoundrySuggestionRequestPayload(string Json, string Hash)
{
    public static MicrosoftFoundrySuggestionRequestPayload From(WorkspaceSuggestionRequestPayload suggestionRequestPayload)
    {
        ArgumentNullException.ThrowIfNull(suggestionRequestPayload);

        var json = JsonSerializer.Serialize(suggestionRequestPayload, MicrosoftFoundrySuggestionAgentJson.Options);

        // Hashed WITHOUT the run's access token: the hash identifies what the run was drawn from, and a
        // per-run credential would make every run's hash unique and the value meaningless.
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(
                suggestionRequestPayload with { McpAccessToken = null },
                MicrosoftFoundrySuggestionAgentJson.Options)))).ToLowerInvariant();

        return new MicrosoftFoundrySuggestionRequestPayload(json, hash);
    }
}
