using AwesomeAssertions;
using CoreRentalNet.Host.Agents;
using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Domain;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// AIWB-17, AIWB-18 and AIWB-47: the adapter's outcomes — a failure is not a refusal, an unconfigured
/// deployment is unavailable rather than open, and no run carries state from the one before it.
/// </summary>
public sealed class SuggestionAgentTests
{
    private const string Refusal =
        """
        { "status": "notWorkspace", "reason": "not about furnishing a workspace", "options": [] }
        """;

    [Fact] // AIWB-17
    public async Task An_agent_that_cannot_be_reached_is_unavailable_rather_than_a_refusal()
    {
        var agent = new FoundrySuggestionAgent(Settings(), _ => throw new InvalidOperationException("no identity"));

        var events = await Collect(agent);

        events.OfType<AgentSuggestionEvent.Unavailable>().Should().ContainSingle()
            .Which.Reason.Should().Contain("no identity");
        events.OfType<AgentSuggestionEvent.Completed>().Should().BeEmpty("a failure is not an answer");
    }

    [Fact] // AIWB-17, the other half: a refusal IS an answer
    public async Task A_refusal_arrives_as_a_completed_result_and_not_as_a_failure()
    {
        var agent = new FoundrySuggestionAgent(Settings(), _ => Tracer(new RecordingChatClient(Refusal)));

        var events = await Collect(agent);

        events.OfType<AgentSuggestionEvent.Unavailable>().Should().BeEmpty();

        var completed = events.OfType<AgentSuggestionEvent.Completed>().Should().ContainSingle().Subject;

        completed.Result.Status.Should().Be(AgentSuggestionStatus.NotWorkspace);
        completed.Result.Reason.Should().NotBeNullOrWhiteSpace();
        completed.Result.Options.Should().BeEmpty();
    }

    [Fact] // AIWB-18
    public void With_no_agent_configured_the_port_answers_unavailable()
    {
        var settings = SuggestionAgentSettings.From(Configuration());

        settings.IsConfigured.Should().BeFalse("the feature is off by default");

        new NoAgentConfigured().StreamAsync(Request(), CancellationToken.None)
            .ToBlockingEnumerable()
            .OfType<AgentSuggestionEvent.Unavailable>()
            .Should().ContainSingle();
    }

    [Fact]
    public void A_missing_endpoint_is_not_configured_and_a_missing_name_falls_back_to_the_deployed_one()
    {
        SuggestionAgentSettings.From(Configuration(
                ("Agent:Enabled", "true"), ("Agent:AgentName", "some-agent"))).IsConfigured
            .Should().BeFalse("the project endpoint is the one setting a deployment must supply");

        // The agent name is fixed by the agent tree's own azure.yaml, so it has a default: a deployment that
        // supplies only the project endpoint is configured, not half-configured.
        var unnamed = SuggestionAgentSettings.From(Configuration(
            ("Agent:Enabled", "true"), ("Agent:ProjectEndpoint", "https://example.invalid")));

        unnamed.IsConfigured.Should().BeTrue();
        unnamed.AgentName.Should().Be("core-rental-workspace-suggestion-agent");
    }

    [Fact]
    public void An_endpoint_with_the_feature_off_is_not_configured()
    {
        SuggestionAgentSettings.From(Configuration(
                ("Agent:Enabled", "false"), ("Agent:ProjectEndpoint", "https://example.invalid"))).IsConfigured
            .Should().BeFalse("the feature is off until a deployment turns it on");
    }

    [Fact] // e05s05 task 6: the cost arrives outside the result and is merged back in
    public async Task The_run_cost_is_read_from_the_object_carrying_it_rather_than_from_the_result()
    {
        const string answer = Refusal +
            "\n{ \"runUsage\": { \"modelCalls\": 2, \"inputTokens\": 172000, \"outputTokens\": 400, " +
            "\"model\": \"gpt-4.1-mini\", \"promptVersion\": \"rephraser.v1+suggestor.v1\" } }";

        var agent = new FoundrySuggestionAgent(Settings(), _ => Tracer(new RecordingChatClient(answer)));

        var completed = (await Collect(agent)).OfType<AgentSuggestionEvent.Completed>().Should().ContainSingle().Subject;

        completed.Result.Status.Should().Be(AgentSuggestionStatus.NotWorkspace);
        completed.Result.RunUsage.Should().NotBeNull();
        completed.Result.RunUsage!.ModelCalls.Should().Be(2);
        completed.Result.RunUsage.InputTokens.Should().Be(172_000);
        completed.Result.RunUsage.PromptVersion.Should().Be("rephraser.v1+suggestor.v1");
    }

    [Fact] // a run whose cost never arrived still has an answer
    public async Task A_result_with_no_run_cost_is_still_an_answer_and_the_cost_is_null()
    {
        var agent = new FoundrySuggestionAgent(Settings(), _ => Tracer(new RecordingChatClient(Refusal)));

        var completed = (await Collect(agent)).OfType<AgentSuggestionEvent.Completed>().Should().ContainSingle().Subject;

        completed.Result.Status.Should().Be(AgentSuggestionStatus.NotWorkspace);
        completed.Result.RunUsage.Should().BeNull("a missing record is the application's to notice, not a reason to withhold an answer");
    }

    [Fact] // the real wire, and the reason the adapter reads the LAST object
    public async Task The_specification_is_not_mistaken_for_the_result()
    {
        // Captured by running the host: the sequential workflow's answer is TWO top-level objects, the
        // rephraser's specification first and the suggestor's result last, separated by a newline. Reading
        // the first one returns a specification - which has no "options" at all.
        const string Spec =
            """
            { "status": "spec", "reason": null, "ceilingMonthly": 1500000,
              "slots": [ { "slot": "Desk", "quantity": 1, "purpose": "a wide, stable surface" } ],
              "constraints": [ "a small room" ] }
            """;

        var agent = new FoundrySuggestionAgent(Settings(), _ => Tracer(new RecordingChatClient(Spec + Refusal)));

        var completed = (await Collect(agent)).OfType<AgentSuggestionEvent.Completed>().Should().ContainSingle().Subject;

        completed.Result.Status.Should().Be(AgentSuggestionStatus.NotWorkspace, "the result was read, not the specification");
        completed.Result.Reason.Should().Be("not about furnishing a workspace");
    }

    [Fact] // AIWB-23: a run the customer stopped is not a run that could not be made
    public async Task A_cancelled_run_is_not_reported_as_unavailable()
    {
        // The failure this guards against is quiet and expensive in support: a customer who pressed Stop is
        // told the suggestion service could not be reached, and the run is recorded as a failure rather than
        // as the cancellation it was.
        using var stopping = new CancellationTokenSource();
        var agent = new FoundrySuggestionAgent(Settings(), _ => Tracer(new StoppingChatClient(stopping)));
        var raised = new List<AgentSuggestionEvent>();

        var act = async () =>
        {
            await foreach (var happened in agent.StreamAsync(Request(), stopping.Token))
            {
                raised.Add(happened);
            }
        };

        // It travels as itself, so the endpoint can end the run without reporting a failure.
        await act.Should().ThrowAsync<OperationCanceledException>();

        raised.OfType<AgentSuggestionEvent.Unavailable>().Should().BeEmpty("stopping is an ending, not a failure");
        raised.OfType<AgentSuggestionEvent.Completed>().Should().BeEmpty("a stopped run has no answer");
    }

    [Fact] // AIWB-47
    public async Task Two_identical_runs_send_identical_payloads_and_carry_nothing_over()
    {
        var client = new RecordingChatClient(Refusal);
        var agent = new FoundrySuggestionAgent(Settings(), _ => Tracer(client));

        _ = await Collect(agent);
        _ = await Collect(agent);

        client.Prompts.Should().HaveCount(2);
        client.Prompts[0].Should().Be(client.Prompts[1], "a run must not carry anything from the run before it");
    }

    [Fact]
    public void The_agent_endpoint_is_derived_from_the_project_endpoint_and_the_name()
    {
        FoundryAgentFactory.AgentEndpoint(Settings()).AbsoluteUri.Should().Be(
            "https://example.invalid/api/projects/probe/agents/core-rental-workspace-suggestion-agent/endpoint/protocols/openai");
    }

    [Fact]
    public async Task The_payload_carries_the_catalogue_the_application_sent_and_nothing_else()
    {
        var client = new RecordingChatClient(Refusal);
        var agent = new FoundrySuggestionAgent(Settings(), _ => Tracer(client));

        _ = await Collect(agent);

        client.Prompts.Single().Should().Contain("DSKB08XN4JDR");
        client.Prompts.Single().Should().Contain("a quiet corner");
    }

    private static SuggestionAgentSettings Settings()
        => SuggestionAgentSettings.From(Configuration(
            ("Agent:Enabled", "true"),
            ("Agent:ProjectEndpoint", "https://example.invalid/api/projects/probe"),
            ("Agent:AgentName", "core-rental-workspace-suggestion-agent")));

    private static IConfiguration Configuration(params (string Key, string Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(pair => pair.Key, pair => (string?)pair.Value))
            .Build();

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
                    new CatalogMetadata(["desk", "workstations"], new Dictionary<string, string>(), ["focused work"], ["a move every month"])),
            ]);

    /// <summary>A MAF agent over a fake model client: the remote agent's shape, with no network.</summary>
    private static Microsoft.Agents.AI.AIAgent Tracer(Microsoft.Extensions.AI.IChatClient client)
        => new Microsoft.Agents.AI.ChatClientAgent(
            client,
            new Microsoft.Agents.AI.ChatClientAgentOptions
            {
                Name = "core-rental-workspace-suggestion-agent",
                ChatOptions = new Microsoft.Extensions.AI.ChatOptions
                {
                    ResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat.ForJsonSchema<AgentSuggestionResult>(),
                },
            });

    private static async Task<List<AgentSuggestionEvent>> Collect(ISuggestionAgent agent)
    {
        var events = new List<AgentSuggestionEvent>();

        await foreach (var raised in agent.StreamAsync(Request(), CancellationToken.None))
        {
            events.Add(raised);
        }

        return events;
    }
}
