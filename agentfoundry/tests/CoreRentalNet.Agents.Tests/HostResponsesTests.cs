using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using AwesomeAssertions;
using Azure.AI.AgentServer.Core;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry.Hosting;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The host serves the agent over the <b>Responses</b> protocol and streams.
/// </summary>
/// <remarks>
/// <para>
/// The agent is backed by a fake chat client, so the host is exercised with no network, no model and no
/// credential — hosting, protocol and SSE lifecycle is what is under test, not the reasoning.
/// </para>
/// <para>
/// Two things were found by writing this rather than assumed: <c>AgentHostApp</c> exposes
/// <c>RunAsync(CancellationToken)</c> but not <c>StartAsync</c>/<c>StopAsync</c>, and the host binds its own
/// port — <b>8088</b> by default — honouring the <c>PORT</c> environment variable and ignoring
/// <c>ASPNETCORE_URLS</c>. The test therefore takes a free port through <c>PORT</c> rather than squatting the
/// fixed one.
/// </para>
/// </remarks>
public sealed class HostResponsesTests
{
    /// <summary>What a stage publishes, as the event stream spells it.</summary>
    private const string PublishedEvent =
        """
        {"type":"candidate","customerWorkflowIdentifier":"run-1","approvedWorkspaceSetup":{"lines":[{"slot":"Desk","sku":"DSKB08XN4JDR","name":"Sit-Stand Desk","quantity":1,"amount":4200000}]}}
        """;

    [Fact]
    public async Task The_host_answers_the_responses_protocol_and_streams()
    {
        var port = FreePort();
        var previous = Environment.GetEnvironmentVariable("PORT");
        Environment.SetEnvironmentVariable("PORT", port.ToString());

        try
        {
            var agent = new ChatClientAgent(
                new FixedChatClient(PublishedEvent),
                new ChatClientAgentOptions { Name = "core-rental-workspace-suggestion-agent" });

            var builder = AgentHost.CreateBuilder([]);
            builder.Services.AddFoundryResponses(agent);
            builder.RegisterProtocol("responses", endpoints => endpoints.MapFoundryResponses());

            var hostApp = builder.Build();

            using var stopping = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var running = hostApp.RunAsync(stopping.Token);

            await WaitForPortAsync(port, TimeSpan.FromSeconds(20));

            using var http = new HttpClient { BaseAddress = new Uri($"http://[::1]:{port}") };
            var response = await http.PostAsJsonAsync("/responses", new { input = "a desk and a chair", stream = true });

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadAsStringAsync();

            body.Should().Contain("candidate", "the event a stage published comes back over the protocol");
            body.Should().Contain("DSKB08XN4JDR", "the sku the event carried survives the round trip");

            await stopping.CancelAsync();
            await Swallow(running);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PORT", previous);
        }
    }

    private static async Task WaitForPortAsync(int port, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var probe = new TcpClient();
                await probe.ConnectAsync(IPAddress.Loopback, port);
                return;
            }
            catch (SocketException)
            {
                await Task.Delay(100);
            }
        }

        throw new TimeoutException($"The agent host did not start listening on {port} within {timeout}.");
    }

    private static async Task Swallow(Task running)
    {
        try
        {
            await running;
        }
        catch (OperationCanceledException)
        {
            // Expected: the test stopped the host.
        }
    }

    /// <summary>A free loopback port, so the test never collides with anything already listening.</summary>
    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        return port;
    }
}
