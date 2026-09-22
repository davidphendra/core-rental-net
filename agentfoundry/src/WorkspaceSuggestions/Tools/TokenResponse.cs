using System.Text.Json.Serialization;

namespace WorkspaceSuggestions.Tools;

/// <summary>The token endpoint's answer, as this agent reads it.</summary>
/// <remarks>
/// Only the two fields the provider is documented to send are read; anything else in the answer is ignored, so
/// a provider that adds a field does not break the agent.
/// </remarks>
internal sealed record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);
