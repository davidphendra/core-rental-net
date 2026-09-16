namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// The machine-readable code that travels with every refused request, so a caller branches on a value
/// rather than on prose.
/// </summary>
/// <remarks>
/// These strings are part of the API's contract: the Microsoft REST API Guidelines say the top-level
/// error codes must be documented, and that a caller should not have to parse a message to decide what
/// to do. They are therefore stable, and adding one is a change to the published contract rather than
/// a detail of the response.
/// </remarks>
internal static class ApiErrorCode
{
    /// <summary>Where the code this request reported is kept, so the response can carry it too.</summary>
    public const string ItemKey = "CoreRentalNet.Api.ErrorCode";

    /// <summary>Answered nothing in particular that this API knows how to explain.</summary>
    public const string InvalidRequest = "api.invalid_request";

    /// <summary>A filter was sent that the catalogue does not publish.</summary>
    public const string UnknownFilter = "catalog.unknown_filter";

    /// <summary>No token was presented, or the one presented was not accepted.</summary>
    public const string Unauthenticated = "catalog.unauthenticated";

    /// <summary>A token was accepted, and it does not entitle the caller to this.</summary>
    public const string NotPermitted = "catalog.not_permitted";

    /// <summary>Nothing answers this path.</summary>
    public const string UnknownRoute = "api.unknown_route";

    /// <summary>The path exists, but not for this method.</summary>
    public const string MethodNotAllowed = "api.method_not_allowed";

    /// <summary>The request failed before it could be answered.</summary>
    public const string Unhandled = "api.unhandled";

    /// <summary>Nothing else describes it.</summary>
    public const string Unclassified = "api.error";

    /// <summary>
    /// The code for this response: what the request named, or what its status leaves.
    /// </summary>
    /// <remarks>
    /// A request that knows what went wrong says so, and that answer is kept; this is how a mistyped
    /// filter is reported as itself rather than as "a 400 happened". Everything else falls back to the
    /// status, which is the most a generic refusal can honestly say.
    /// </remarks>
    public static string For(int? status, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Items.TryGetValue(ItemKey, out var reported) && reported is string code)
        {
            return code;
        }

        return status switch
        {
            StatusCodes.Status400BadRequest => InvalidRequest,
            StatusCodes.Status401Unauthorized => Unauthenticated,
            StatusCodes.Status403Forbidden => NotPermitted,
            StatusCodes.Status404NotFound => UnknownRoute,
            StatusCodes.Status405MethodNotAllowed => MethodNotAllowed,
            StatusCodes.Status500InternalServerError => Unhandled,
            _ => Unclassified,
        };
    }

    /// <summary>Names the code for the rest of this request.</summary>
    public static void Set(HttpContext context, string code)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Items[ItemKey] = code;
    }
}
