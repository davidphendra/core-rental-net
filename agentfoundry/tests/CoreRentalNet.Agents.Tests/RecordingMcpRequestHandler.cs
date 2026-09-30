using System.Net;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The catalogue server's own end of the wire: it answers every request and keeps what it was sent.</summary>
/// <remarks>
/// <b>Written by hand rather than substituted, because what it must preserve is the request that left the
/// handler.</b> The assertion these tests make is about a header on a real <see cref="HttpRequestMessage"/>, so the
/// double is a real terminal handler and the only thing it decides is the answer.
/// </remarks>
internal sealed class RecordingMcpRequestHandler : HttpMessageHandler
{
    /// <summary>Every request that reached the far end, in order.</summary>
    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}"),
        });
    }
}
