using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.Host.Agents;
using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Domain;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The one path the browser tier rests on: the <b>real</b> adapter, through the real client and the real
/// streaming code, reading a stand-in on this machine with no credential.
/// </summary>
/// <remarks>
/// <para>
/// <b>This exists because the path was lost once and nothing noticed.</b> The application used to build its
/// agent one of two ways, chosen by the scheme: a Foundry project over https with the application's identity,
/// or a stand-in over http with a placeholder key and no credential. The restructure kept only the first, and
/// the epic's own plan went on saying the browser tier runs against a local stand-in - so every browser
/// scenario in this epic became unbuildable while the record still read as though it were not.
/// </para>
/// <para>
/// The two paths are not a convenience. Measured rather than assumed: asked to reach a plain-http endpoint, the
/// project client refuses with <c>Bearer token authentication is not permitted for non TLS protected (https)
/// endpoints</c>. There is no credential that can be presented to a stand-in, so a stand-in cannot be reached
/// the other way even in principle.
/// </para>
/// <para>
/// The stand-in below is deliberately small - it serves the wire and nothing else. The shared one the browser
/// suite drives, with the scenario switch tests choose an answer with, is the next step; what is guarded here
/// is the transport and the reading, which is what silently broke.
/// </para>
/// </remarks>
public sealed class StandInTransportTests
{
    /// <summary>
    /// The answer a sequential workflow gives: the rephraser's specification first, the suggestor's result last.
    /// </summary>
    /// <remarks>
    /// The order is the whole point of the assertion below. Reading the first object yields a specification,
    /// which has no options at all - so a stand-in that answered with the result alone would leave the adapter's
    /// most expensive lesson untested.
    /// </remarks>
    private const string Answer =
        """{ "status": "spec", "reason": null, "ceilingMonthly": 1500000, "slots": [ { "slot": "Desk", "quantity": 1, "purpose": "a wide, stable surface" } ], "constraints": [] }""" +
        "\n" +
        """{ "status": "suggested", "reason": null, "options": [ { "lines": [ { "slot": "Desk", "sku": "DSKB08XN4JDR", "quantity": 1, "why": "a stable surface" } ], "rationale": "A calm, focused setup." } ] }""";

    [Fact] // the browser tier's foundation: no credential, no TLS, the real adapter
    public async Task The_real_adapter_reads_a_stand_in_on_this_machine_with_no_credential()
    {
        var (standIn, url) = await StartStandInAsync(Answer);

        try
        {
            var settings = Local(url);

            settings.IsLocal().Should().BeTrue("a plain-http endpoint is a stand-in, and the scheme is what says so");

            var events = await Collect(settings);

            var completed = events.OfType<AgentSuggestionEvent.Completed>().Should().ContainSingle().Subject;

            completed.Result.Status.Should().Be(
                AgentSuggestionStatus.Suggested,
                "the result was read, not the specification that arrived before it");

            completed.Result.Options.Should().ContainSingle();
            completed.Result.Options[0].Rationale.Should().Be("A calm, focused setup.");
            completed.PayloadHash.Should().NotBeNullOrWhiteSpace();

            events.OfType<AgentSuggestionEvent.Unavailable>().Should().BeEmpty("nothing failed");
        }
        finally
        {
            await standIn.StopAsync();
            await standIn.DisposeAsync();
        }
    }

    [Fact] // the client half of the assumption e05s01 recorded as unproven, measured rather than argued
    public async Task A_json_schema_response_format_travels_natively_on_the_responses_path()
    {
        string? sent = null;
        var (standIn, url) = await StartStandInAsync(Answer, captured: body => sent = body);

        try
        {
            // The PRODUCTION client - not a replica of it - because the format is the client's translation of
            // the contract, and a test that built its own client would be testing itself.
            var agent = new ChatClientAgent(
                FoundryAgentFactory.LocalChat(Local(url)),
                new ChatClientAgentOptions
                {
                    Name = "wire",
                    ChatOptions = new ChatOptions
                    {
                        ResponseFormat = ChatResponseFormat.ForJsonSchema<AgentSuggestionResult>(),
                    },
                });

            await foreach (var _ in agent.RunStreamingAsync("a desk and a chair"))
            {
                // The answer is not what this asserts.
            }

            sent.Should().NotBeNull("the client sent a request for the stand-in to capture");

            using var request = JsonDocument.Parse(sent!);
            var format = request.RootElement.GetProperty("text").GetProperty("format");

            // The protocol's own structured-output field, carrying the contract the agent declared. This is the
            // half of the assumption that never needed a deployment: the client does put the schema on the wire,
            // and it does it natively rather than by the synthetic-tool route a fallback would have used.
            format.GetProperty("type").GetString().Should().Be("json_schema");
            format.GetProperty("name").GetString().Should().Be(nameof(AgentSuggestionResult));

            format.GetProperty("schema").GetProperty("properties")
                .EnumerateObject().Select(property => property.Name)
                .Should().BeEquivalentTo(["status", "reason", "options"]);

            // No tool: structured output is the format, not a function the model is made to call.
            if (request.RootElement.TryGetProperty("tools", out var tools))
            {
                tools.GetArrayLength().Should().Be(0, "the schema is a format, not a synthetic tool call");
            }

            // NOT asserted: `strict`. Measured, it is absent from the format, so conformance is not enforced by
            // the API and a non-conforming answer is a real possibility - which is what the contract validation
            // in e05s08 and the reader's tolerance exist to absorb. Pinned here as knowledge rather than as an
            // assertion, because a future client that set it would be an improvement, not a regression.
        }
        finally
        {
            await standIn.StopAsync();
            await standIn.DisposeAsync();
        }
    }

    [Fact] // the split is the scheme, so a project endpoint keeps the credential path
    public void A_project_endpoint_is_not_local_and_an_endpoint_on_this_machine_is()
    {
        Local("http://127.0.0.1:5000").IsLocal().Should().BeTrue();

        // https is a Foundry project: the bearer path, the application's own identity, and no stand-in.
        Local("https://example.invalid/api/projects/probe").IsLocal().Should().BeFalse();

        // And nothing configured is not local either, so an unconfigured deployment cannot fall into the
        // credential-free path by accident.
        SuggestionAgentSettings.From(new ConfigurationBuilder().Build()).IsLocal().Should().BeFalse();
    }

    private static SuggestionAgentSettings Local(string endpoint)
        => SuggestionAgentSettings.From(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Agent:Enabled"] = "true",
                ["Agent:ProjectEndpoint"] = endpoint,
                ["Agent:AgentName"] = "stand-in",
            })
            .Build());

    private static async Task<List<AgentSuggestionEvent>> Collect(SuggestionAgentSettings settings)
    {
        var events = new List<AgentSuggestionEvent>();

        await foreach (var raised in new FoundrySuggestionAgent(settings).StreamAsync(Request(), CancellationToken.None))
        {
            events.Add(raised);
        }

        return events;
    }

    /// <summary>
    /// A stand-in that serves the Responses protocol and nothing else: one created event, the text in pieces,
    /// then the completed response. Written from the protocol rather than from the client, because it is the
    /// other end of the wire.
    /// </summary>
    private static async Task<(WebApplication App, string Url)> StartStandInAsync(
        string answer,
        Action<string>? captured = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        var app = builder.Build();

        app.MapPost("/responses", async (HttpContext context) =>
        {
            if (captured is not null)
            {
                using var reader = new StreamReader(context.Request.Body);
                captured(await reader.ReadToEndAsync());
            }

            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";

            await context.Response.WriteAsync(
                "event: response.created\ndata: {\"type\":\"response.created\",\"sequence_number\":0,\"response\":{\"id\":\"resp_1\",\"object\":\"response\",\"created_at\":0,\"status\":\"in_progress\",\"model\":\"stand-in\",\"output\":[]}}\n\n");

            var sequence = 1;

            // Line by line, because a real agent streams and one piece would leave the accumulation across
            // chunks untested - the case that fails on the runs with the most output.
            foreach (var line in answer.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                await Delta(context, line + "\n", sequence++);
            }

            await context.Response.WriteAsync(
                $"event: response.completed\ndata: {{\"type\":\"response.completed\",\"sequence_number\":{sequence},\"response\":{{\"id\":\"resp_1\",\"object\":\"response\",\"created_at\":0,\"status\":\"completed\",\"model\":\"stand-in\",\"output\":[{{\"id\":\"msg_1\",\"type\":\"message\",\"status\":\"completed\",\"role\":\"assistant\",\"content\":[{{\"type\":\"output_text\",\"text\":{JsonSerializer.Serialize(answer + "\n")},\"annotations\":[]}}]}}]}}}}\n\n");

            await context.Response.Body.FlushAsync();
        });

        var port = FreePort();
        var url = $"http://127.0.0.1:{port}";
        app.Urls.Add(url);

        await app.StartAsync();

        return (app, url);
    }

    private static async Task Delta(HttpContext context, string piece, int sequence)
    {
        var delta = JsonSerializer.Serialize(new
        {
            type = "response.output_text.delta",
            sequence_number = sequence,
            item_id = "msg_1",
            output_index = 0,
            content_index = 0,
            delta = piece,
        });

        await context.Response.WriteAsync($"event: response.output_text.delta\ndata: {delta}\n\n");
    }

    private static SuggestionRequest Request()
        => new(
            "run-1",
            "a quiet corner for two monitors",
            "IDR",
            null,
            [new SuggestionSlotRule(SlotId.Desk, 1), new SuggestionSlotRule(SlotId.Monitor, 3)],
            [
                new CompactCatalogItem(
                    "DSKB08XN4JDR",
                    "HON Mod Desk Shell, 60 x 30 x 29, Mahogany",
                    CatalogCategory.Desk,
                    null,
                    266_000m,
                    "This 60 inch desk shell is part of the HON Mod Desk Collection.",
                    new CatalogMetadata(["desk"], new Dictionary<string, string>(), ["focused work"], [])),
            ]);

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        return port;
    }
}
