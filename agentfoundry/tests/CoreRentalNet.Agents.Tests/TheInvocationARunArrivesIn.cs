using CoreRentalNet.Agents.Shared.Mcp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The HTTP request a run arrives in, as the agent's own code sees it.</summary>
/// <remarks>
/// <b>Hand-written rather than substituted, because what it must preserve is that a header read is a header
/// read.</b> The reader's whole job is to look at the request the hosting layer handed it, so the double is a real
/// ASP.NET Core request with real headers on it, and the only thing the test decides is which ones.
/// </remarks>
internal static class TheInvocationARunArrivesIn
{
    /// <summary>The reader as a run gets it, for an invocation carrying this header.</summary>
    public static AccessTokenHeaderReader Carrying(string headerName, string headerValue)
        => Carrying(NullLoggerFactory.Instance, headerName, headerValue);

    /// <summary>The same, logging through the caller's factory so the reader's own line can be read back.</summary>
    public static AccessTokenHeaderReader Carrying(
        ILoggerFactory loggerFactory,
        string headerName,
        string headerValue)
        => ReaderOver(RequestCarrying((headerName, headerValue)), loggerFactory);

    /// <summary>The reader as a run gets it, for an invocation carrying no caller token.</summary>
    public static AccessTokenHeaderReader CarryingNothing()
        => ReaderOver(RequestCarrying(), NullLoggerFactory.Instance);

    private static AccessTokenHeaderReader ReaderOver(
        IHttpContextAccessor httpContextAccessor,
        ILoggerFactory loggerFactory)
        => new(httpContextAccessor, loggerFactory.CreateLogger<AccessTokenHeaderReader>());

    private static IHttpContextAccessor RequestCarrying(params (string HeaderName, string HeaderValue)[] headers)
    {
        var httpContext = new DefaultHttpContext();

        foreach (var (headerName, headerValue) in headers)
        {
            httpContext.Request.Headers[headerName] = headerValue;
        }

        return new HttpContextAccessor { HttpContext = httpContext };
    }
}
