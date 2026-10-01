using System.Net;

namespace CoreRentalNet.Host.Tests;

/// <summary>The far end of a token handler's request: it answers, and keeps what it was sent.</summary>
/// <remarks>
/// Written by hand rather than substituted, because what it must preserve is the request that left the handler -
/// the assertion these tests make is about its <c>Authorization</c> header.
/// </remarks>
internal sealed class RecordingOutboundRequestHandler : HttpMessageHandler
{
    /// <summary>The request that reached the far end, or null when none did.</summary>
    public HttpRequestMessage? Request { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Request = request;

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}"),
        });
    }
}
