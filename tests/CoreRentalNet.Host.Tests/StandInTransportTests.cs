using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.E2E.LocalAgent;
using CoreRentalNet.Host.Agents;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The path the browser tier rests on: the <b>real</b> adapter, through the real client and the real
/// streaming code, reading the stand-in on this machine with no credential.
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
/// endpoints</c>. There is no credential that can be presented to a stand-in.
/// </para>
/// <para>
/// The stand-in is the same one the browser suite starts as a process - one agent, two ways to run it - so
/// what is proved here is proved about the thing the browser tier will drive rather than about a copy of it.
/// </para>
/// </remarks>
public sealed class StandInTransportTests
{
    [Fact] // the browser tier's foundation: no credential, no TLS, the real adapter
    public async Task The_real_adapter_reads_a_stand_in_on_this_machine_with_no_credential()
    {
        var (standIn, url) = await StartStandInAsync("suggested");

        try
        {
            var settings = Local(url);

            settings.IsLocal().Should().BeTrue("a plain-http endpoint is a stand-in, and the scheme is what says so");

            var events = await Collect(settings);
            var completed = events.OfType<AgentSuggestionEvent.Completed>().Should().ContainSingle().Subject;

            completed.Result.Status.Should().Be(
                AgentSuggestionStatus.Suggested,
                "the result was read, not the specification that arrived before it");

            completed.Result.Options.Should().HaveCount(3);
            completed.Result.Options[0].Rationale.Should().Be(
                "An uncluttered setup for one person, kept inside a small room.");
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
        var (standIn, url) = await StartStandInAsync("suggested", captured: body => sent = body);

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
            // half of the assumption that never needed a deployment.
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

            // NOT asserted: `strict`. Measured, it is absent, so the API does not enforce conformance and a
            // non-conforming answer is a real possibility - which is what contract validation in e05s08 and the
            // reader's tolerance exist to absorb. Recorded as knowledge rather than pinned as an assertion,
            // because a client that set it would be an improvement, not a regression.
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

    [Fact] // the premise the browser test's strip assertion rests on
    public void The_scenarios_that_exist_to_be_dirty_are_dirty()
    {
        // AIWB-26 asserts that streamed text carries no price and no link. That assertion is only worth
        // anything if the text arrived WITH one, so the fixture's own dirt is checked here - beside the
        // SKU check, for the same reason: a fixture that is quietly clean makes a test of the application
        // into a test of the fixture.
        ScenarioLibrary.For("leaky").Should().Contain("Rp1.206.000");
        ScenarioLibrary.For("leaky").Should().Contain("https://example.invalid/pricing");

        // And the truncated scenario has to stop before the result closes, or there would be no run that
        // says something and then fails - which is the state AIWB-29 is about.
        var truncated = ScenarioLibrary.For("truncated");

        truncated.Should().Contain("\"why\":\"a wide, stable surface\"", "the lines are readable");
        truncated.TrimEnd().Should().NotEndWith("}", "and the answer never closes");
    }

    [Fact] // a fixture may only name SKUs the catalogue actually holds
    public void Every_scenario_names_a_SKU_the_catalogue_has()
    {
        // A stand-in holding a SKU the catalogue does not would exercise the drop path in EVERY scenario, and
        // the happy path would be tested nowhere. That is the kind of failure a fixture makes possible, and
        // this is the test that makes it impossible - it is why the stand-in's SKUs are real ones.
        var catalogue = CatalogueSkus();

        catalogue.Should().NotBeEmpty("the catalogue is the file the application loads, and it has to be readable here");
        ScenarioLibrary.Skus.Should().NotBeEmpty("guards against the rule passing because the stand-in names nothing");

        var missing = ScenarioLibrary.Skus.Where(sku => !catalogue.Contains(sku, StringComparer.Ordinal)).ToArray();

        missing.Should().BeEmpty($"the stand-in names SKUs the catalogue does not have: {string.Join(", ", missing)}");
    }

    private static IReadOnlyList<string> CatalogueSkus()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);

        while (root is not null && !File.Exists(Path.Combine(root.FullName, "CoreRentalNet.sln")))
        {
            root = root.Parent;
        }

        var path = Path.Combine(
            root?.FullName ?? throw new InvalidOperationException("Could not locate the repository root."),
            "src",
            "shared",
            "data",
            "products.json");

        using var document = JsonDocument.Parse(File.ReadAllText(path));

        var rows = document.RootElement.ValueKind is JsonValueKind.Array
            ? document.RootElement
            : document.RootElement.GetProperty("products");

        return [.. rows.EnumerateArray().Select(row => row.GetProperty("skuNo").GetString()!)];
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

    /// <summary>The shared stand-in, in this process, on a port the test chose.</summary>
    private static async Task<(WebApplication App, string Url)> StartStandInAsync(
        string scenario,
        Action<string>? captured = null)
    {
        var app = LocalAgentApp.Create([], captured).Build();

        LocalAgentEndpoints.Map(app);

        var port = FreePort();
        var url = $"http://127.0.0.1:{port}";
        app.Urls.Add(url);

        await app.StartAsync();

        using var http = new HttpClient();

        var chosen = await http.PostAsJsonAsync($"{url}/scenario", new { name = scenario });

        chosen.IsSuccessStatusCode.Should().BeTrue($"the stand-in has a scenario named '{scenario}'");

        return (app, url);
    }

    private static SuggestionRequest Request()
        => new(
            "run-1",
            "a quiet corner for two monitors",
            "IDR",
            null,
            [new SuggestionSlotRule(Modules.Workspace.Domain.SlotId.Desk, 1)],
            [
                new Presentation.CompactCatalogItem(
                    "DSKB08XN4JDR",
                    "HON Mod Desk Shell, 60 x 30 x 29, Mahogany",
                    Modules.Catalog.Application.Contracts.CatalogCategory.Desk,
                    null,
                    266_000m,
                    "This 60 inch desk shell is part of the HON Mod Desk Collection.",
                    new Modules.Catalog.Application.Contracts.CatalogMetadata(
                        ["desk"],
                        new Dictionary<string, string>(),
                        ["focused work"],
                        [])),
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
