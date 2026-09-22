using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Mcp;

/// <summary>Turns a failure a tool cannot avoid into a message that is safe to log and safe to throw.</summary>
/// <remarks>
/// <para>
/// <b>The operator's reason never crosses.</b> A similarity failure carries the vector file's path, which is
/// written for whoever runs the ingestion tool, and the REST endpoint deliberately keeps it out of its body.
/// The detail goes to the log; the exception thrown in its place carries only the sentence below. Measured, the
/// MCP server answers a thrown tool with a generic line of its own rather than the exception's message, so the
/// sentence is a second line of defence rather than the model's wording - but it is the one that would be shown
/// if that ever changed.
/// </para>
/// <para>
/// The original exception is not attached, so no serialiser can reach its message by another route.
/// </para>
/// </remarks>
internal static class CatalogToolFailure
{
    /// <summary>The sentence thrown in place of the operator's reason: logged, and safe to show.</summary>
    public const string SimilarityUnavailable =
        "The catalogue cannot be searched by meaning right now. Do not compose an answer from memory; "
        + "tell the customer the catalogue is unavailable.";

    /// <summary>The exception to throw in its place: logged in full, safe to show.</summary>
    public static Exception Refuse(Exception exception, ILogger logger, string tool)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(logger);

        logger.LogWarning(exception, "{Tool} could not answer because the catalogue is unavailable", tool);

        return new InvalidOperationException(SimilarityUnavailable);
    }
}
