namespace CoreRentalNet.Agents.Shared.Mcp;

internal static class McpRunScope
{
    private static readonly AsyncLocal<McpRunContext?> s_currentContext = new();

    public static McpRunContext? Current
    {
        get => s_currentContext.Value;
        set => s_currentContext.Value = value;
    }

    /// <summary>Enters a token context for one catalogue operation and restores the previous one afterwards.</summary>
    public static IDisposable Enter(string? callerAccessToken)
    {
        var previousTokenContext = Current;

        Current = string.IsNullOrWhiteSpace(callerAccessToken)
            ? null
            : new McpRunContext(AccessTokenHeader.Name, callerAccessToken);

        return new McpRunScopeLease(previousTokenContext);
    }
}
