using AwesomeAssertions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CoreRentalNet.Agents.Shared.Agents;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using Xunit;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Tools;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Requests;
using CoreRentalNet.Agents.Shared.ChatClients;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The workflow graph: the verifier gates the run, the stages run in order, and the run ends in one terminal
/// status with the approved setups streamed before it.
/// </summary>
/// <remarks>
/// Both halves of the streaming design are load-bearing and are proven here rather than assumed: hosting serves
/// a workflow as an agent only when its start executor speaks the chat protocol, and a stage's yielded message
/// reaches the caller because the agent is built with the workflow outputs in the response.
/// </remarks>
public sealed class WorkflowTests
{
    private const string Request =
        """
        { "runId": "run-1", "query": "a quiet corner for one screen", "currency": "IDR", "ceilingMonthly": 1500000,
          "slots": [ { "slot": "Desk", "capacity": 1 } ] }
        """;

    private const string Verification = """{ "isWorkspaceRequest": true, "refusalReason": null }""";

    private static readonly string Expansion = WorkspaceRequirementExpansionFixtures.Json();

    private const string Retrieval =
        """
        { "isAvailable": true, "unavailableReason": null,
          "searches": [ { "tool": "search_catalogue", "category": "desk", "found": 1, "reason": null } ] }
        """;

    /// <summary>What the reranker answers: the desk, and nothing for the components nobody asked for.</summary>
    private const string Reranking =
        """
        { "categories": {
            "desk": [ { "sku": "DSKB08XN4JDR", "relevance": "high" } ],
            "chair": [], "monitor": [], "lamp": [], "plant": [], "bean_bag": [], "coffee_machine": [] } }
        """;

    /// <summary>One answer the catalogue's name search gave, recorded as a tool would have returned it.</summary>
    private const string RecordedCatalogueAnswer =
        """
        { "matches": { "value": [ { "sku": "DSKB08XN4JDR", "category": "desk", "name": "Sit-Stand Desk",
                                    "subCategory": null, "description": "A height-adjustable sitting or standing desk.",
                                    "pricePerMonth": 420000 } ] },
          "cheapestProductIgnoringTheCeiling": null }
        """;

    private const string Composition =
        """
        { "setups": [ { "lines": [ { "slot": "Desk", "sku": "DSKB08XN4JDR", "name": "Sit-Stand Desk",
                                      "quantity": 1, "amount": 420000 } ] } ] }
        """;

    private const string Review = """{ "isAcceptable": true, "issues": [], "summary": null }""";

    private const string RejectedReview = """{ "isAcceptable": false, "issues": [ { "issueCode": "SETUPS_NOT_DISTINCT", "requiredCorrectionDescription": "the setups are near-duplicates" } ], "summary": null }""";

    [Fact]
    public async Task The_workflow_runs_every_stage_and_streams_an_approved_setup_before_its_ending()
    {
        var agent = Build(Verification, Expansion, Retrieval, Reranking, Composition, Review);

        var response = await agent.RunAsync(Request);
        var text = response.Text!;

        text.Should().Contain("\"type\":\"stageStarted\"", "each stage announces that it began");
        text.Should().Contain("\"type\":\"candidate\"", "the approved setup is streamed as its own event");
        text.Should().Contain("DSKB08XN4JDR", "the setup the composer produced survives to the caller");
        text.Should().Contain("\"runStatus\":\"success\"");

        text.IndexOf("\"type\":\"candidate\"", StringComparison.Ordinal)
            .Should().BeLessThan(
                text.IndexOf("\"type\":\"completed\"", StringComparison.Ordinal),
                "the setups arrive before the run's ending, never after it");
    }

    [Fact]
    public async Task A_sentence_that_is_not_a_workspace_request_is_rejected_before_any_other_stage()
    {
        var client = new ScriptedChatClient(
            """{ "isWorkspaceRequest": false, "refusalReason": "not about furnishing a workspace" }""");

        var text = (await Build(client).RunAsync(Request)).Text!;

        text.Should().Contain("\"runStatus\":\"rejected\"");
        text.Should().NotContain("\"type\":\"candidate\"");
        client.Requests.Should().HaveCount(1, "only the verifier ran, because the run ended at the gate");
    }

    [Fact]
    public async Task Six_stage_agents_are_asked_once_each_on_the_successful_path()
    {
        var client = new ScriptedChatClient(Verification, Expansion, Retrieval, Reranking, Composition, Review);

        _ = await Build(client).RunAsync(Request);

        client.Requests.Should().HaveCount(
            6, "verifier, rephraser, retriever, reranker, composer, reviewer");
    }

    [Fact] // the roster identities are what a checkpoint records, so they are pinned as literals
    public void Every_stage_identity_is_stable()
    {
        WorkspaceSuggestionAgentRoster.All.Select(profile => profile.Name).Should().Equal(
            "workspace-request-verifier",
            "workspace-requirement-rephraser",
            "catalogue-product-retriever",
            "catalogue-candidate-reranker",
            "workspace-setup-composer",
            "workspace-setup-reviewer");
    }

    [Fact] // the token arrives out of band, so the run needs nothing in its message
    public async Task A_run_reads_the_callers_token_off_the_invocation()
    {
        const string TheCallersCatalogueToken = "header.eyJzdWIiOiJjdXN0b21lci0xIn0.signature";

        var loggerFactory = new RunLogRecordingLoggerFactory();
        var accessTokens = new McpAccessTokenService(loggerFactory.CreateLogger<McpAccessTokenService>());
        var agent = Build(
            new ScriptedChatClient(Verification, Expansion, Retrieval, Reranking, Composition, Review),
            loggerFactory,
            accessTokens,
            TheInvocationARunArrivesIn.Carrying(loggerFactory, AccessTokenHeader.Name, TheCallersCatalogueToken));

        // The request carries no token at all: everything this run was given came off the invocation.
        var text = (await agent.RunAsync(Request)).Text!;

        text.Should().Contain("\"runStatus\":\"success\"");
        loggerFactory.RecordedLines.Should().Contain(
            line => line.Contains("token present on the invocation", StringComparison.Ordinal),
            "the invocation is where the token was read from");
        loggerFactory.RecordedLines.Should().NotContain(
            line => line.Contains(TheCallersCatalogueToken, StringComparison.Ordinal));
    }

    [Fact] // the platform's forwarding rule, on this side of the wire
    public void The_header_the_application_states_carries_the_platforms_client_prefix()
        => AccessTokenHeader.Name.Should().StartWith(
            "x-client-",
            "the hosting layer forwards client headers only under this prefix, and a dropped token is silent");

    public static TheoryData<string, string[]> EveryEndingOfARun() => new()
    {
        { "success", [Verification, Expansion, Retrieval, Reranking, Composition, Review] },
        { "rejected", ["""{ "isWorkspaceRequest": false, "refusalReason": "not about furnishing a workspace" }"""] },
        {
            "unavailable",
            [
                Verification, Expansion, Retrieval, Reranking, Composition, RejectedReview,
                Expansion, Retrieval, Reranking, Composition, RejectedReview,
                Expansion, Retrieval, Reranking, Composition, RejectedReview,
            ]
        },
    };

    [Theory] // whichever way a run ends, the credential it borrowed is given up before the ending is announced
    [MemberData(nameof(EveryEndingOfARun))]
    public async Task Every_ending_releases_the_callers_token(string runStatus, string[] replies)
    {
        const string TheCallersCatalogueToken = "header.eyJzdWIiOiJjdXN0b21lci0xIn0.signature";

        var loggerFactory = new RunLogRecordingLoggerFactory();

        // Built from the recording factory, so the holder's own release line is among the lines this test reads.
        var accessTokens = new McpAccessTokenService(loggerFactory.CreateLogger<McpAccessTokenService>());
        var agent = Build(
            new ScriptedChatClient(replies),
            loggerFactory,
            accessTokens,
            TheInvocationARunArrivesIn.Carrying(
                loggerFactory, AccessTokenHeader.Name, TheCallersCatalogueToken));

        var text = (await agent.RunAsync(Request)).Text!;

        text.Should().Contain($"\"runStatus\":\"{runStatus}\"");
        accessTokens.Token.Should().BeNull(
            "the ending releases it, which is earlier than the call's scope being disposed");
        loggerFactory.RecordedLines.Should().Contain(
            line => line.Contains("was released", StringComparison.Ordinal),
            "the point the credential stops being used is written down rather than implied by a lifetime");
    }

    [Fact] // the leak nobody notices: a credential in a log line travels further than the request body does
    public async Task No_log_line_a_run_writes_contains_the_callers_catalogue_token()
    {
        const string TheCallersCatalogueToken = "header.eyJzdWIiOiJjdXN0b21lci0xIn0.signature";

        var loggerFactory = new RunLogRecordingLoggerFactory();
        var agent = Build(
            new ScriptedChatClient(Verification, Expansion, Retrieval, Reranking, Composition, Review),
            loggerFactory);

        _ = await agent.RunAsync(Request.Replace(
            "\"slots\"",
            $"\"mcpAccessToken\": \"{TheCallersCatalogueToken}\", \"slots\"",
            StringComparison.Ordinal));

        loggerFactory.RecordedLines.Should().NotBeEmpty("a run writes log lines, so this assertion means something");
        loggerFactory.RecordedLines.Should().NotContain(
            line => line.Contains(TheCallersCatalogueToken, StringComparison.Ordinal),
            "the token is presented to the catalogue, never written down");
    }

    [Fact] // only the retriever searches, so only it is offered the catalogue's tools
    public void Only_the_retrieval_stage_is_offered_the_catalogue_tools()
    {
        WorkspaceSuggestionAgentRoster.All.Where(profile => profile.UsesCatalogueTools).Select(profile => profile.Name)
            .Should().Equal("catalogue-product-retriever");
    }

    [Fact] // every node a run reaches writes one started line and one completed line, and only those nodes
    public async Task The_console_trace_names_every_node_a_run_reached()
    {
        var runLog = new RunLogRecordingLoggerFactory();
        var agent = Build(
            new ScriptedChatClient(Verification, Expansion, Retrieval, Reranking, Composition, Review),
            runLog);

        _ = await agent.RunAsync(Request);

        string[] reachedNodes =
        [
            "verify-workspace-request", "rephrase-workspace-requirement", "retrieve-catalogue-products",
            "build-candidate-product-pool", "rerank-workspace-candidates", "compose-workspace-setups",
            "validate-workspace-setup-structure", "review-workspace-setups",
            "complete-workspace-suggestion-success",
        ];

        foreach (var node in reachedNodes)
        {
            runLog.RecordedLines.Should().Contain(
                line => line.Contains($"Node {node} started.", StringComparison.Ordinal),
                $"the run reached {node} and its start is written down");
            runLog.RecordedLines.Should().Contain(
                line => line.Contains($"Node {node} completed.", StringComparison.Ordinal),
                $"the run reached {node} and its completion is written down");
        }

        runLog.RecordedLines.Should().Contain(
            line => line.Contains("Node read-workspace-suggestion-request completed. Result:", StringComparison.Ordinal),
            "the entry node reports the request it read");
        runLog.RecordedLines.Should().NotContain(
            line => line.Contains("Node read-workspace-suggestion-request started.", StringComparison.Ordinal),
            "the entry node speaks the chat protocol through its own base, so it has no start line of its own");
        runLog.RecordedLines.Should().NotContain(
            line => line.Contains("decide-workspace-setup-retry", StringComparison.Ordinal),
            "a node the run never reached writes nothing");
    }

    [Fact] // each node's completion line carries the result it produced
    public async Task The_console_trace_carries_each_node_result()
    {
        var runLog = new RunLogRecordingLoggerFactory();
        var agent = Build(
            new ScriptedChatClient(Verification, Expansion, Retrieval, Reranking, Composition, Review),
            runLog);

        _ = await agent.RunAsync(Request);

        runLog.RecordedLines.Should().Contain(
            line => line.Contains("Node verify-workspace-request completed. Result:", StringComparison.Ordinal));
        runLog.RecordedLines.Should().Contain(
            line => line.Contains("\"isWorkspaceRequest\":true", StringComparison.Ordinal),
            "the verifier's own verdict is what its line reports");
        runLog.RecordedLines.Should().Contain(
            line => line.Contains("Node validate-workspace-setup-structure completed. Result: true", StringComparison.Ordinal));
        runLog.RecordedLines.Should().Contain(
            line => line.Contains("Node complete-workspace-suggestion-success completed. Result: \"success\"", StringComparison.Ordinal));
    }

    [Fact] // the retriever's own verdict ends the run: nothing downstream can search a catalogue it could not
    public async Task A_retriever_that_reports_the_catalogue_unavailable_ends_the_run_before_the_pool()
    {
        var runLog = new RunLogRecordingLoggerFactory();
        var client = new ScriptedChatClient(
            Verification,
            Expansion,
            """{ "isAvailable": false, "unavailableReason": "the catalogue is unreachable", "searches": [] }""");
        var agent = Build(client, runLog);

        var text = (await agent.RunAsync(Request)).Text!;

        text.Should().Contain("\"runStatus\":\"unavailable\"");
        text.Should().Contain("the catalogue is unreachable");
        client.Requests.Should().HaveCount(3, "the verifier, the rephraser and the retriever, and no retry");
        runLog.RecordedLines.Should().NotContain(
            line => line.Contains("Node build-candidate-product-pool", StringComparison.Ordinal),
            "the pool is never built from a catalogue that could not be searched");
    }

    [Fact] // a catalogue that returned nothing ends the run rather than composing from an empty pool
    public async Task An_empty_catalogue_ends_the_run_before_the_reranker()
    {
        var runLog = new RunLogRecordingLoggerFactory();
        var client = new ScriptedChatClient(Verification, Expansion, Retrieval)
        {
            // No tool answered, so the pool the tools' answers build is empty.
            AfterEachReply = () => { },
        };
        var agent = Build(client, runLog);

        var text = (await agent.RunAsync(Request)).Text!;

        text.Should().Contain("\"runStatus\":\"unavailable\"");
        text.Should().Contain("The catalogue returned no products.");
        client.Requests.Should().HaveCount(3, "the verifier, the rephraser and the retriever, and no retry");
        runLog.RecordedLines.Should().NotContain(
            line => line.Contains("Node rerank-workspace-candidates", StringComparison.Ordinal),
            "the reranker is never reached with nothing to rank");
    }

    [Fact] // a refused tools/list is a run ending Unavailable, not a failed run
    public async Task A_catalogue_that_refuses_the_call_ends_the_run_unavailable()
    {
        var runLog = new RunLogRecordingLoggerFactory();
        var tokens = new McpAccessTokenService(NullLogger<McpAccessTokenService>.Instance)
        {
            Token = "the-callers-token",
        };
        var client = new ScriptedChatClient(Verification, Expansion);
        var agent = Build(client, runLog, tokens, catalogue: new RefusingMcpAuthorizationConnection());

        var text = (await agent.RunAsync(Request)).Text!;

        text.Should().Contain("\"runStatus\":\"unavailable\"");
        text.Should().Contain("The catalogue could not be reached.");
        client.Requests.Should().HaveCount(
            2, "the verifier and the rephraser; the retriever never reached a model");
    }

    [Fact] // the loop is bounded: an attempt that fails the structure check still counts
    public async Task A_run_whose_setups_never_validate_ends_unavailable_after_the_attempts_run_out()
    {
        const string NoSetups = """{ "setups": [] }""";

        string[] replies =
        [
            Verification,
            Expansion, Retrieval, Reranking, NoSetups,
            Expansion, Retrieval, Reranking, NoSetups,
            Expansion, Retrieval, Reranking, NoSetups,
        ];
        var client = new ScriptedChatClient(replies);

        var text = (await Build(client).RunAsync(Request)).Text!;

        text.Should().Contain("\"runStatus\":\"unavailable\"");
        text.Should().Contain("\"completedAttemptCount\":3");
        client.Requests.Should().HaveCount(
            13, "the verifier once, then three attempts of rephraser, retriever, reranker and composer");
    }

    [Fact] // a run that ends at the gate logs the two nodes it reached and none of the ones it skipped
    public async Task A_run_that_ends_at_the_gate_logs_only_the_nodes_it_reached()
    {
        var runLog = new RunLogRecordingLoggerFactory();
        var agent = Build(
            new ScriptedChatClient("""{ "isWorkspaceRequest": false, "refusalReason": "not about furnishing a workspace" }"""),
            runLog);

        _ = await agent.RunAsync(Request);

        runLog.RecordedLines.Should().Contain(
            line => line.Contains("Node verify-workspace-request completed.", StringComparison.Ordinal));
        runLog.RecordedLines.Should().Contain(
            line => line.Contains("Node complete-workspace-suggestion-rejection completed.", StringComparison.Ordinal));
        runLog.RecordedLines.Should().NotContain(
            line => line.Contains("Node rephrase-workspace-requirement", StringComparison.Ordinal),
            "the run ended at the gate, so the retryable half of the graph never ran");
    }


    [Fact]
    public async Task A_rejected_review_returns_to_rephrasing_and_the_third_rejection_ends_unavailable()
    {
        const string Rejected = """{ "isAcceptable": false, "issues": [ { "issueCode": "SLOT_PURPOSE_UNSATISFIED", "requiredCorrectionDescription": "the desk purpose is not answered" } ], "summary": null }""";

        // One attempt is rephraser, retriever, composer, reviewer; the verifier runs once.
        string[] replies =
        [
            Verification, Expansion, Retrieval, Reranking, Composition, Rejected,
            Expansion, Retrieval, Reranking, Composition, Rejected,
            Expansion, Retrieval, Reranking, Composition, Rejected,
        ];
        var client = new ScriptedChatClient(replies);

        var text = (await Build(client).RunAsync(Request)).Text!;

        text.Should().Contain("\"type\":\"retry\"", "each rejected attempt tells the caller another is beginning");
        text.Should().Contain("\"runStatus\":\"unavailable\"");
        text.Should().Contain("\"completedAttemptCount\":3");
        client.Requests.Should().HaveCount(
            16, "the verifier ran once and the three-attempt cycle ran three times");
    }

    [Fact]
    public async Task A_second_attempt_that_passes_ends_the_run_successfully()
    {
        const string Rejected = """{ "isAcceptable": false, "issues": [ { "issueCode": "SETUPS_NOT_DISTINCT", "requiredCorrectionDescription": "the setups are near-duplicates" } ], "summary": null }""";

        string[] replies =
        [
            Verification, Expansion, Retrieval, Reranking, Composition, Rejected,
            Expansion, Retrieval, Reranking, Composition, Review,
        ];

        var text = (await Build(new ScriptedChatClient(replies)).RunAsync(Request)).Text!;

        text.Should().Contain("\"type\":\"retry\"");
        text.Should().Contain("\"runStatus\":\"success\"");
        text.Should().Contain("\"completedAttemptCount\":2");
    }

    [Fact] // a setup set that could not be composed never reaches the reviewer, so the searches are the feedback
    public async Task A_retry_whose_attempt_composed_nothing_is_told_what_the_searches_reported()
    {
        // The path this pins: the retriever comes back with nothing because the budget excluded everything, so
        // the composer composes nothing, the structure validator fails, and the graph routes straight to the
        // retry — the reviewer never runs and writes no issues. What the searches reported is therefore the only
        // account of what went wrong, and the second reading has to be shown it.
        const string NothingAffordable =
            """
            { "isAvailable": true, "unavailableReason": null,
              "searches": [ { "tool": "search_catalogue", "category": "desk", "found": 0,
                              "reason": "nothing at or below 200000; the cheapest matching desk is 240000" } ],
              "products": [] }
            """;

        const string NoSetups = """{ "setups": [] }""";

        string[] replies =
        [
            Verification, Expansion, NothingAffordable, Reranking, NoSetups,
            Expansion, Retrieval, Reranking, Composition, Review,
        ];
        var client = new ScriptedChatClient(replies);

        _ = await Build(client).RunAsync(Request);

        client.Requests.Should().HaveCount(
            10, "one attempt composed nothing and the next one succeeded");
        client.Requests[5].Should().Contain(
            "the cheapest matching desk is 240000",
            "the second reading is told the figure the search reported, or it cannot correct the allocation");
    }

    [Fact] // the retry feedback is the previous attempt's issues, and only those
    public async Task The_rephrasing_attempt_is_shown_the_previous_review_issues()
    {
        const string Rejected = """{ "isAcceptable": false, "issues": [ { "issueCode": "MISSING_SLOT", "requiredCorrectionDescription": "the seating is missing" } ], "summary": null }""";

        string[] replies =
        [
            Verification, Expansion, Retrieval, Reranking, Composition, Rejected,
            Expansion, Retrieval, Reranking, Composition, Review,
        ];
        var client = new ScriptedChatClient(replies);

        _ = await Build(client).RunAsync(Request);

        client.Requests[6].Should().Contain(
            "the seating is missing", "the second reading sees what the first missed");
    }


    [Fact] // a console sends a sentence, not the application's envelope, and must not be refused for it
    public async Task A_console_sentence_is_read_as_a_query_rather_than_refused()
    {
        var client = new ScriptedChatClient(Verification, Expansion, Retrieval, Reranking, Composition, Review);

        var text = (await Build(client).RunAsync("a desk and a chair")).Text!;

        text.Should().Contain("\"runStatus\":\"success\"");
        client.Requests[0].Should().Contain("a desk and a chair", "the sentence a console typed is the query");
    }

    private static AIAgent Build(params string[] replies) => Build(new ScriptedChatClient(replies));

    private static AIAgent Build(ScriptedChatClient client) => Build(client, NullLoggerFactory.Instance);

    private static AIAgent Build(ScriptedChatClient client, ILoggerFactory loggerFactory)
        => Build(client, loggerFactory, new McpAccessTokenService(NullLogger<McpAccessTokenService>.Instance));

    private static AIAgent Build(
        ScriptedChatClient client,
        ILoggerFactory loggerFactory,
        IMcpAccessTokenService tokens,
        AccessTokenHeaderReader? callerAccessTokenHeaderReader = null,
        IMcpAuthorizationConnection? catalogue = null)
    {
        catalogue ??= new McpAuthorizationConnection(
            new McpSetting(string.Empty),
            tokens,
            NullLoggerFactory.Instance,
            NullLogger<McpAuthorizationConnection>.Instance);

        var recordedToolAnswers = new McpToolAnswerLedger();

        // A scripted client calls no tool, so the answer a tool would have given is recorded for it: without one
        // the pool would be empty and the composer would be handed no products at all. It is recorded through the
        // arguments a retriever would have sent, so the pool's own mapping decides which component the products
        // belong to — the path a real run takes, with the catalogue's answer supplied by the test.
        client.AfterEachReply ??= () => recordedToolAnswers.Record(
            "search_catalogue",
            new Dictionary<string, object?> { ["category"] = "desk", ["subCategory"] = null },
            RecordedCatalogueAnswer);

        var modelCallTelemetry = new TelemetryChatClient(
            new GuardrailChatClient(client, tokens),
            "gpt-4.1-mini",
            "test-prompts",
            loggerFactory.CreateLogger<TelemetryChatClient>());
        var stageAgents = new WorkspaceSuggestionAgentBuilder(
            tokens, catalogue, modelCallTelemetry, TestGuardrails.Middleware, TestGuardrails.AllowList, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(), loggerFactory);

        var executorBuilder = new WorkspaceSuggestionExecutorBuilder(
            stageAgents,
            tokens,
            callerAccessTokenHeaderReader ?? TheInvocationARunArrivesIn.CarryingNothing(),
            new WorkspaceSuggestionWorkflowOptions(),
            recordedToolAnswers,
            modelCallTelemetry,
            loggerFactory);

        return new WorkspaceSuggestionWorkflow(
            executorBuilder,
            new WorkspaceSuggestionAgentIdentity { AgentName = "core-rental-workspace-suggestion-agent" })
            .AsAIAgent();
    }
}
