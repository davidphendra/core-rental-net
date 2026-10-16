namespace CoreRentalNet.Agents.Shared.Mcp;

internal static class McpRunScope
{
    private static readonly AsyncLocal<McpRunContext?> s_current = new();

    public static McpRunContext? Current
    {
        get => s_current.Value;
        set => s_current.Value = value;
    }
}
