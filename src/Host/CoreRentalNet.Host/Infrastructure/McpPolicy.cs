namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// The name of the policy that guards the MCP endpoint, in one place.
/// </summary>
/// <remarks>
/// A name rather than a value: the endpoint that maps it, the registration that decides it and the tests that
/// re-point it all refer to one rule. It requires an authenticated caller and nothing more - which tools that
/// caller may use is decided per tool, by the same policies the REST endpoints carry - and it names the bearer
/// scheme only where one is registered, for the reason the API's own policy gives.
/// </remarks>
internal static class McpPolicy
{
    public const string Name = "McpCaller";
}
