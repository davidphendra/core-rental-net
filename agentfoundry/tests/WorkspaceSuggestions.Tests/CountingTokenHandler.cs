using System.Net;
using System.Text;

namespace WorkspaceSuggestions.Tests;

/// <summary>A token endpoint that counts its calls and answers with a token of a chosen lifetime.</summary>
/// <remarks>
/// The lifetime is the parameter because the refresh rule is a comparison with the clock, and a lifetime of
/// zero puts the token inside the margin without any test having to move time.
/// </remarks>
internal sealed class CountingTokenHandler(int expiresIn) : HttpMessageHandler
{
    /// <summary>How many times the endpoint was asked.</summary>
    public int Requests { get; private set; }

    /// <summary>What the last grant sent, so a test can assert it carried the audience.</summary>
    public string LastBody { get; private set; } = string.Empty;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests++;

        LastBody = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$"""{"access_token":"token-{{Requests}}","expires_in":{{expiresIn}}}""",
                Encoding.UTF8,
                "application/json"),
        };
    }
}
