using AwesomeAssertions;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// Spike: can a hand-wired workflow graph whose start executor takes the caller's chat messages be served as one
/// <see cref="AIAgent"/>, and do its yielded messages arrive as the response text? The whole streaming design
/// rests on both halves being true, so this is written before the graph is built rather than after.
/// </summary>
public sealed class WorkflowGraphSpikeTests
{
    [Fact]
    public async Task A_custom_graph_served_as_an_agent_streams_its_yielded_messages()
    {
        var startExecutor = new YieldingStartExecutor();
        var startBinding = startExecutor.BindExecutor();

        var workflow = new WorkflowBuilder(startBinding)
            .WithOutputFrom(startBinding)
            .Build();

        var agent = workflow.AsAIAgent(name: "spike-agent", includeWorkflowOutputsInResponse: true);

        var response = await agent.RunAsync("a desk and a chair");

        response.Text.Should().Contain("EVENT-ONE", "a yielded message crosses as response text");
        response.Text.Should().Contain("EVENT-TWO", "and every yielded message crosses, in order");

        startExecutor.MessagesReceived.Should().BeGreaterThan(0, "the caller's chat message reached the start executor");
    }

    [Fact]
    public async Task A_custom_graph_streams_its_yielded_messages_as_separate_updates()
    {
        var startExecutor = new YieldingStartExecutor();
        var startBinding = startExecutor.BindExecutor();

        var workflow = new WorkflowBuilder(startBinding)
            .WithOutputFrom(startBinding)
            .Build();

        var agent = workflow.AsAIAgent(name: "spike-agent", includeWorkflowOutputsInResponse: true);

        var fragments = new List<string>();

        await foreach (var update in agent.RunStreamingAsync("a desk and a chair"))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                fragments.Add(update.Text);
            }
        }

        fragments.Should().Contain("EVENT-ONE");
        fragments.Should().Contain("EVENT-TWO");
    }

    /// <summary>A chat-protocol start executor that takes the caller's messages and yields two outputs.</summary>
    private sealed class YieldingStartExecutor()
        : ChatProtocolExecutor("yielding-start")
    {
        public int MessagesReceived { get; private set; }

        protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
            => base.ConfigureProtocol(protocolBuilder).YieldsOutput<ChatMessage>();

        protected override async ValueTask TakeTurnAsync(
            List<ChatMessage> messages,
            IWorkflowContext context,
            bool? emitEvents,
            CancellationToken cancellationToken = default)
        {
            MessagesReceived = messages.Count;

            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, "EVENT-ONE"), cancellationToken);
            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, "EVENT-TWO"), cancellationToken);
        }
    }
}
