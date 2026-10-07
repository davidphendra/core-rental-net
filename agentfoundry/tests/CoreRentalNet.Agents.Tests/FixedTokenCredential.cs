using Azure.Core;

namespace CoreRentalNet.Agents.Tests;

/// <summary>A credential that hands back a throwaway token, so a test can build a client without Azure.</summary>
internal sealed class FixedTokenCredential : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        => new("test-token", DateTimeOffset.MaxValue);

    public override ValueTask<AccessToken> GetTokenAsync(
        TokenRequestContext requestContext, CancellationToken cancellationToken)
        => ValueTask.FromResult(new AccessToken("test-token", DateTimeOffset.MaxValue));
}
