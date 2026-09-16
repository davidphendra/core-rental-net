using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// Fills in the parts of a problem-details answer that a caller needs in order to report it and to
/// decide what to do next.
/// </summary>
/// <remarks>
/// <para>
/// Applied to every problem the application produces - the framework's validation refusal, the status
/// code middleware's body for an empty 401 or 404, and the exception handler's 500 - so a caller
/// writes one parser and gets the same members whichever way the request was refused.
/// </para>
/// <para>
/// <c>traceId</c> is the one field that makes a caller's report actionable: it is the identifier the
/// server logged the request under, so support can find the request from the message alone. It is only
/// added when the framework has not already supplied one, because the framework's is the identifier
/// that was actually logged under.
/// </para>
/// <para>
/// Nothing here exposes the failure itself. The exception handler's <c>detail</c> is the framework's
/// own wording rather than the exception's message, because an exception message is written for a
/// developer reading a log and routinely carries a path, a query or a value that the caller was not
/// necessarily entitled to see.
/// </para>
/// </remarks>
internal static class ApiProblemDetails
{
    public static void Customize(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var problem = context.ProblemDetails;
        var http = context.HttpContext;

        problem.Status ??= http.Response.StatusCode;
        problem.Instance ??= http.Request.Path.ToString();
        problem.Extensions.TryAdd("traceId", Activity.Current?.Id ?? http.TraceIdentifier);
        problem.Extensions["code"] = ApiErrorCode.For(problem.Status, http);
    }
}
