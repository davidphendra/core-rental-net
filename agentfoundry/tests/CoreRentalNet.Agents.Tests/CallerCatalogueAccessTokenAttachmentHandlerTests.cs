using System.Net.Http.Headers;
using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Mcp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The one place a run's token becomes an outbound header, and the rules it is attached under.</summary>
/// <remarks>
/// <para>
/// <b>Tested without a network, because what is asserted is what left the handler.</b> A bearer token is a
/// credential for exactly one server, so the cases that matter are the ones where it is attached — and the ones
/// where the handler refuses to send at all rather than sending unauthenticated.
/// </para>
/// <para>
/// <b>The provider is read per request rather than captured once.</b> That is what makes a refreshed token the one
/// that is presented without rebuilding the agent or the connection, and it is asserted here rather than assumed.
/// </para>
/// </remarks>
public sealed class CallerCatalogueAccessTokenAttachmentHandlerTests
{
    private const string TheCatalogueServer = "https://catalogue.example/mcp";
    private const string TheCallersToken = "header.eyJzdWIiOiJjdXN0b21lci0xIn0.signature";

    [Fact]
    public async Task An_approved_request_carries_the_callers_token_as_a_bearer()
    {
        var (handler, server, _) = HandlerFor(TheCatalogueServer, TheCallersToken);

        using var client = new HttpClient(handler);

        await SendAsync(client, $"{TheCatalogueServer}/messages");

        server.Requests.Should().ContainSingle()
            .Which.Headers.Authorization.Should().Be(new AuthenticationHeaderValue("Bearer", TheCallersToken));
    }

    [Fact] // the token is read from the provider per request, so the newest value is the one that is sent
    public async Task The_token_is_read_afresh_on_every_request()
    {
        var (handler, server, tokens) = HandlerFor(TheCatalogueServer, "the-first-token");

        using var client = new HttpClient(handler);

        await SendAsync(client, $"{TheCatalogueServer}/messages");

        tokens.Token = "the-second-token";

        await SendAsync(client, $"{TheCatalogueServer}/messages");

        server.Requests.Select(request => request.Headers.Authorization?.Parameter)
            .Should().Equal("the-first-token", "the-second-token");
    }

    [Theory]
    [InlineData("https://elsewhere.example/mcp")] // another origin
    [InlineData("http://catalogue.example/mcp")]  // the configured host over plain text
    public async Task A_request_the_catalogue_server_did_not_ask_for_is_refused_rather_than_sent_unauthenticated(
        string requestUri)
    {
        var (handler, server, _) = HandlerFor(TheCatalogueServer, TheCallersToken);

        using var client = new HttpClient(handler);

        var act = async () => await SendAsync(client, requestUri);

        await act.Should().ThrowAsync<InvalidOperationException>();

        server.Requests.Should().BeEmpty(
            "a mis-wired transport is refused at the handler rather than answered by the far end");
    }

    [Fact] // a credential, and a log line travels further than a request does
    public async Task No_log_line_the_handler_writes_contains_the_callers_token()
    {
        var loggerFactory = new RunLogRecordingLoggerFactory();
        var handler = new CallerCatalogueAccessTokenAttachmentHandler(
            HolderHolding(TheCallersToken),
            PolicyFor(TheCatalogueServer),
            loggerFactory.CreateLogger<CallerCatalogueAccessTokenAttachmentHandler>())
        {
            InnerHandler = new RecordingMcpRequestHandler(),
        };

        using var client = new HttpClient(handler);

        await SendAsync(client, $"{TheCatalogueServer}/messages");

        loggerFactory.RecordedLines.Should().NotBeEmpty(
            "the handler writes a line per attachment, so this assertion means something");
        loggerFactory.RecordedLines.Should().NotContain(
            line => line.Contains(TheCallersToken, StringComparison.Ordinal),
            "the origin and the method are safe to record and the token is not");
    }

    /// <summary>Sends one request through the client as the transport would, and reads the status.</summary>
    private static async Task SendAsync(HttpClient client, string requestUri)
    {
        using var response = await client.SendAsync(
            new HttpRequestMessage(HttpMethod.Post, new Uri(requestUri)), CancellationToken.None);

        response.EnsureSuccessStatusCode();
    }

    private static (CallerCatalogueAccessTokenAttachmentHandler Handler, RecordingMcpRequestHandler Server,
        McpAccessTokenService Tokens) HandlerFor(string catalogueServer, string token)
    {
        var tokens = HolderHolding(token);
        var server = new RecordingMcpRequestHandler();

        var handler = McpAuthenticationHelper.BuildTheTokenAttachmentHandler(
            tokens,
            PolicyFor(catalogueServer),
            NullLoggerFactory.Instance);

        // The terminal handler is the far end in this test, rather than a socket that would go looking for one.
        handler.InnerHandler = server;

        return (handler, server, tokens);
    }

    private static CatalogueServerRequestPolicy PolicyFor(string catalogueServer)
        => new(new CallerAuthorisedMcpSettings(catalogueServer));

    private static McpAccessTokenService HolderHolding(string token)
        => new(NullLogger<McpAccessTokenService>.Instance) { Token = token };
}
