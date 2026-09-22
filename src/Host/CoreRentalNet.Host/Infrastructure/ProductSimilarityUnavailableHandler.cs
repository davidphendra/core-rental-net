using CoreRentalNet.Modules.Catalog.Application.Contracts;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// Answers a similarityService search that could not be made with the one shape the API refuses with.
/// </summary>
/// <remarks>
/// <para>
/// <b>A 503 rather than a 500.</b> The vectors are derived data that an operator rebuilds with the ingestion
/// tool, and a caller may retry once they have - which is a service that is temporarily unavailable, not a
/// request this application got wrong.
/// </para>
/// <para>
/// The reason the index could not be read is written for an operator, so it stays out of the body: the status
/// and the <c>code</c> are what a caller branches on, and the trace id is what makes the report findable.
/// </para>
/// </remarks>
internal sealed class ProductSimilarityUnavailableHandler(IProblemDetailsService problemDetails)
    : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (exception is not ProductSimilarityUnavailableException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        ApiErrorCode.Set(httpContext, ApiErrorCode.SimilarityUnavailable);

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "The catalogue cannot be searched by similarityService.",
                Detail = "The stored catalogue vectors are not available to this deployment.",
            },
        });
    }
}
