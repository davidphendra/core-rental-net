namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>The catalogue refused this call, so no search can be made and no setup can be composed.</summary>
/// <remarks>
/// <b>Typed, because the outcome is a run ending Unavailable and not a failed run.</b> A refused token or an
/// unreachable endpoint is the caller's to see the same way an empty catalogue is, so the feature catches this
/// one type and ends the run rather than letting a transport fault escape as a stack trace. The fault itself is
/// still logged where it happened, with the endpoint and never the token.
/// </remarks>
internal sealed class CatalogueUnavailableException(string mcpEndpoint, Exception innerException)
    : Exception($"The catalogue at '{mcpEndpoint}' refused this call.", innerException)
{
    /// <summary>Where the refusal came from, for a log line that names the endpoint and nothing secret.</summary>
    public string McpEndpoint { get; } = mcpEndpoint;
}
