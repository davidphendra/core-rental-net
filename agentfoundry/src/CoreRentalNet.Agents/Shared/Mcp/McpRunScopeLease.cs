namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>Restores the ambient token context when the catalogue operation that needed it ends.</summary>
internal sealed class McpRunScopeLease(
    McpRunContext? previousTokenContext) : IDisposable
{
    public void Dispose() => McpRunScope.Current = previousTokenContext;
}
