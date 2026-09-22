using Microsoft.Extensions.AI;

namespace WorkspaceSuggestions.Tools;

/// <summary>The catalogue's tools, as the composition and the agents see them.</summary>
/// <remarks>
/// A port rather than the MCP client itself, so the composition can hand the suggestor tools without depending
/// on the protocol that found them, and a test can hand it a stand-in.
/// </remarks>
internal interface ICatalogTools : IAsyncDisposable
{
    /// <summary>The tools, empty when this deployment has not been told where the catalogue is.</summary>
    IReadOnlyList<AITool> Tools { get; }
}
