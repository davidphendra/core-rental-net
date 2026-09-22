using CoreRentalNet.Host.Mcp;

namespace CoreRentalNet.Host.Extentions;

/// <summary>Registers the MCP server the catalogue's tools are published over.</summary>
/// <remarks>
/// <para>
/// A third surface over the same handlers the pages and the REST API call: what is new is only the protocol,
/// so there is no second answer to "find me a desk".
/// </para>
/// <para>
/// <b>Per-tool authorization is the policy pipeline, not a second rule.</b> <c>AddAuthorizationFilters</c> makes
/// the server honour each tool's <c>[Authorize]</c>: <c>tools/list</c> is filtered to what the caller may use
/// and <c>tools/call</c> is refused for the rest, both through the same authorization service and the same
/// policies the REST endpoints declare.
/// </para>
/// </remarks>
internal static class McpRegistrationExtentions
{
    public static void AddCatalogMcp(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services
            .AddMcpServer()
            .WithHttpTransport()
            .AddAuthorizationFilters()
            .WithTools<SearchCatalogueTool>()
            .WithTools<SearchSimilarityCatalogueTool>();
    }
}
