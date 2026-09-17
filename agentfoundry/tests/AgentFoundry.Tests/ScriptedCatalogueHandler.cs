using System.Net;

namespace AgentFoundry.Tests;

/// <summary>A catalogue endpoint that answers what a test told it to, and remembers the request.</summary>
/// <remarks>
/// Hand-written, like every other fake here, and a <see cref="HttpMessageHandler"/> rather than a fake
/// HTTP client so that the request the reader builds - its method, its URL, its authorization header -
/// is the thing under test rather than something assumed.
/// </remarks>
internal sealed class ScriptedCatalogueHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    public Uri? Requested { get; private set; }

    public IReadOnlyList<string> Authorizations => authorizations;

    public int Calls { get; private set; }

    private readonly List<string> authorizations = [];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requested = request.RequestUri;
        authorizations.Add(request.Headers.Authorization?.ToString() ?? string.Empty);
        Calls++;

        return Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        });
    }
}
