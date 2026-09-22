namespace CoreRentalNet.Host.Configs;

/// <summary>The MCP endpoint's route, named once.</summary>
/// <remarks>
/// Deliberately outside <c>/api</c>. The API's response pipeline turns a failure into problem details so a
/// caller can branch on a code, and MCP answers in JSON-RPC: a problem-details body is not a JSON-RPC error,
/// so a fault on this endpoint must not be reshaped by that middleware.
/// </remarks>
internal static class McpRoutes
{
    /// <summary>The one path every MCP client connects to.</summary>
    public const string Path = "/mcp";
}
