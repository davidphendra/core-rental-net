using System.ClientModel.Primitives;
using Azure.Core;

namespace CoreRentalNet.Host.Agents;

/// <summary>
/// Puts a fresh access token on every request, and refuses to put one on an insecure one.
/// </summary>
/// <remarks>
/// <para>
/// The token is the application's own identity rather than a stored secret, which is why it is fetched
/// per request instead of held: an access token expires, and a client that captured one at start-up
/// would fail hours later with nothing to say about why.
/// </para>
/// <para>
/// The refusal replicates the guard the Foundry project client applies, and is the reason this exists
/// at all: a bearer token on a plain <c>http://</c> endpoint is sent in clear text to whoever is
/// listening. Failing before the request is built means a misconfigured endpoint fails on the first
/// call rather than leaking a token on every one. The local stand-in therefore uses an api key and
/// never installs this policy.
/// </para>
/// </remarks>
internal sealed class ApplicationTokenPolicy(TokenCredential credential, string scope) : PipelinePolicy
{
    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        => ProcessAsync(message, pipeline, currentIndex).AsTask().GetAwaiter().GetResult();

    public override async ValueTask ProcessAsync(
        PipelineMessage message,
        IReadOnlyList<PipelinePolicy> pipeline,
        int currentIndex)
    {
        ArgumentNullException.ThrowIfNull(message);

        var uri = message.Request.Uri!;

        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                $"A bearer token is never sent to an endpoint that is not TLS protected: '{uri}'.");
        }

        var token = await credential
            .GetTokenAsync(new TokenRequestContext([scope]), CancellationToken.None)
            .ConfigureAwait(false);

        message.Request.Headers.Set("Authorization", $"Bearer {token.Token}");

        await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);
    }
}
