using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CoreRentalNet.Host.Agents;

/// <summary>The exact bytes sent to the agent, and a hash of them.</summary>
/// <remarks>
/// The hash is computed over the payload <b>actually sent</b> rather than over the catalogue file, so a change
/// to the projection itself — dropping a field, adding one — is captured as well as a change to a product. It
/// is what lets the run record say what a suggestion was drawn from (`e05s09`).
/// </remarks>
internal sealed record SuggestionPayload(string Json, string Hash)
{
    public static SuggestionPayload From(SuggestionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var json = JsonSerializer.Serialize(request, SuggestionJson.Options);

        // Hashed WITHOUT the run's access token: the hash identifies what the run was drawn from, and a
        // per-run credential would make every run's hash unique and the value meaningless.
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(request with { McpAccessToken = null }, SuggestionJson.Options)))).ToLowerInvariant();

        return new SuggestionPayload(json, hash);
    }
}
