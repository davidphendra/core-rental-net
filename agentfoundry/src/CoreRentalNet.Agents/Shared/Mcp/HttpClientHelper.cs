using System.Security.Cryptography.X509Certificates;

namespace CoreRentalNet.Agents.Shared.Mcp;

public static class HttpClientHelper
{
    private static readonly TimeSpan CallTimeout = TimeSpan.FromMinutes(5);

    private static readonly TimeSpan PooledConnectionLifetime = TimeSpan.FromMinutes(2);

    internal static HttpClient CreateWithPolicy(
        CatalogueServerRequestPolicy catalogueServerRequestPolicy,
        ILoggerFactory loggerFactory)
    {
        var accessTokenDelegationHandler = CreateTokenAttachmentHandler(
            catalogueServerRequestPolicy,
            loggerFactory);
        var httpClient = new HttpClient(accessTokenDelegationHandler)
        {
            Timeout = CallTimeout,
        };

        return httpClient;
    }

    /// <summary>The handler that attaches the token, over the terminal handler it delegates to.</summary>
    private static McpAccessTokenHandler CreateTokenAttachmentHandler(
            CatalogueServerRequestPolicy catalogueServerRequestPolicy,
            ILoggerFactory loggerFactory)
        // A DelegatingHandler must be given the terminal handler it delegates to, or the first request fails
        // with "The inner handler has not been assigned."
        => new(
            catalogueServerRequestPolicy,
            loggerFactory.CreateLogger<McpAccessTokenHandler>())
        {
            InnerHandler = new SocketsHttpHandler
            {
                UseCookies = false,
                AllowAutoRedirect = false,
                PooledConnectionLifetime = PooledConnectionLifetime,

                // The MAF sample states this as HttpClientHandler.CheckCertificateRevocationList;
                // SocketsHttpHandler — which pools better, and is what this transport uses — carries the same
                // setting on its TLS options, and its default is to check nothing.
                SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                {
                    CertificateRevocationCheckMode = X509RevocationMode.Online,
                },
            },
        };
}