using Microsoft.Extensions.Logging;

namespace AgentFoundry.WorkspaceSuggestions.Intent;

/// <summary>
/// The line written when the intent table does not know a phrasing.
/// </summary>
/// <remarks>
/// The miss rate is the only thing that tells anyone the table has stopped being useful. Without it the
/// model fallback becomes the normal path and nobody finds out, which is the failure this file exists
/// to make visible. The request is not logged - only that the table missed, and how long the request
/// was, because the request is the customer's own words.
/// </remarks>
internal static class IntentLog
{
    /// <summary>The logger category the line is written under, so a test can find it.</summary>
    public const string Category = "AgentFoundry.WorkspaceSuggestions.Intent";

    public static void Missed(ILogger logger, string requestId, int queryLength)
        => logger.LogInformation(
            "Intent table missed for request {RequestId}; the slot set was inferred from a {QueryLength}-character request.",
            requestId,
            queryLength);
}
