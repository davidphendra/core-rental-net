using System.Runtime.CompilerServices;
using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Tools;
using CoreRentalNet.Agents.Shared.ChatClients;
using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;
using CoreRentalNet.Agents.Shared.Guardrails.Pipeline;
using CoreRentalNet.Agents.Shared.Guardrails.Results;
using CoreRentalNet.Agents.Shared.Mcp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The path a catalogue tool actually takes: offered by the client, invoked by the function loop, and run through
/// the guardrail pipeline.
/// </summary>
/// <remarks>
/// <b>This is the seam that was unproven.</b> The agent's function middleware never sees a tool that the chat
/// client attaches inside its own chain, so the pipeline would never run and the ledger would stay empty — which
/// is what made the pool unavailable. This harness drives the real function loop over the real client so that a
/// future move of the wrapping point fails here rather than in a run.
/// </remarks>
public sealed class CatalogueToolGuardrailTests
{
    private const string CatalogueAnswer =
        """
        { "matches": { "value": [ { "sku": "DSKB08XN4JDR", "category": "desk", "name": "Sit-Stand Desk",
                                    "subCategory": null, "description": "A height-adjustable desk.",
                                    "pricePerMonth": 420000 } ] },
          "cheapestProductIgnoringTheCeiling": null }
        """;

    [Fact]
    public async Task A_tool_offered_through_the_client_runs_and_reaches_the_ledger()
    {
        var ledger = new McpToolAnswerLedger();
        var explored = false;

        var client = BuildClient(
            ledger,
            argumentGuards: [],
            () =>
            {
                explored = true;
                return CatalogueAnswer;
            });

        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "a desk")]);

        explored.Should().BeTrue("the guarded tool is still the tool the function loop invokes");
        ledger.RecordedAnswers.Should().ContainSingle()
            .Which.ToolName.Should().Be(CatalogueSearchToolNames.NameSearch);
        ledger.RecordedAnswers[0].AnswerText.Should().Be(
            CatalogueAnswer,
            "the ledger holds the tool's own answer, which is what the pool is later built from");
    }

    [Fact]
    public async Task A_refused_call_never_runs_the_tool_and_records_nothing()
    {
        var ledger = new McpToolAnswerLedger();
        var explored = false;

        var client = BuildClient(
            ledger,
            argumentGuards: [new RefusingArgumentGuard()],
            () =>
            {
                explored = true;
                return CatalogueAnswer;
            });

        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "a desk")]);

        explored.Should().BeFalse("a guard that refuses a call stops it before the tool runs");
        ledger.RecordedAnswers.Should().BeEmpty("a refused call has no answer to record");
    }

    private static AuthorisedMcpChatClient BuildClient(
        McpToolAnswerLedger ledger,
        IReadOnlyList<IToolGuard> argumentGuards,
        Func<string> catalogue)
    {
        var tool = AIFunctionFactory.Create(
            (string category) => catalogue(),
            CatalogueSearchToolNames.NameSearch);

        var pipeline = new ToolGuardPipeline(
            argumentGuards,
            [new ToolResultRecordingGuard(ledger)]);

        var tokens = new McpAccessTokenService(NullLogger<McpAccessTokenService>.Instance)
        {
            Token = "the-callers-token",
        };

        return new AuthorisedMcpChatClient(
            new FunctionInvokingChatClient(new OneToolCallThenAnswer(), NullLoggerFactory.Instance),
            new StubRunScope(
                (typeof(IMcpAccessTokenService), tokens),
                (typeof(IMcpAuthorizationConnection), new OfferingMcpAuthorizationConnection(tool)),
                (typeof(IToolGuardPipeline), pipeline)),
            true,
            TestGuardrails.AllowList,
            NullLogger<AuthorisedMcpChatClient>.Instance);
    }

    /// <summary>A catalogue that lists the one tool the test hands it.</summary>
    private sealed class OfferingMcpAuthorizationConnection(params AITool[] tools) : IMcpAuthorizationConnection
    {
        public bool IsConfigured => true;

        public Task<IReadOnlyList<AITool>> ToolsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<AITool>>(tools);

        public ValueTask CloseAsync() => ValueTask.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>A model that asks for the catalogue tool once and then answers.</summary>
    private sealed class OneToolCallThenAnswer : IChatClient
    {
        private int _call;

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var call = Interlocked.Increment(ref _call);

            var message = call == 1
                ? new ChatMessage(
                    ChatRole.Assistant,
                    [new FunctionCallContent(
                        "call-1",
                        CatalogueSearchToolNames.NameSearch,
                        new Dictionary<string, object?> { ["category"] = "desk" })])
                : new ChatMessage(ChatRole.Assistant, "the desk is available");

            return Task.FromResult(new ChatResponse(message));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var response = await GetResponseAsync(messages, options, cancellationToken);

            foreach (var message in response.Messages)
            {
                yield return new ChatResponseUpdate(message.Role, message.Contents);
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private sealed class RefusingArgumentGuard : IToolGuard
    {
        public ValueTask<GuardrailDecision> BeforeAsync(ToolInvocation invocation, CancellationToken cancellationToken)
            => ValueTask.FromResult(GuardrailDecision.Deny("the call was refused"));
    }
}
