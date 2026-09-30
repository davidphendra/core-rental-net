using AwesomeAssertions;
using Microsoft.Agents.AI.Workflows;
using CoreRentalNet.Agents.Features.EchoReply;
using CoreRentalNet.Agents.Features.EchoReply.Agents;
using CoreRentalNet.Agents.Features.EchoReply.Stages.Routing;
using CoreRentalNet.Agents.Shared.Agents;
using CoreRentalNet.Agents.Shared.Prompts;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The echo feature end to end: its prompt travels with the binary, its roster builds a stage, and the workflow
/// served as an agent answers with the caller's message.
/// </summary>
public sealed class EchoReplyWorkflowTests
{
    [Fact]
    public void The_echo_prompt_is_embedded_and_readable()
    {
        var source = new EmbeddedInstructionSource(EchoReplyAgentRoster.EchoReply.PromptFileName);

        source.Text.Should().NotBeNullOrWhiteSpace();
        source.Text.Should().Contain("verbatim", "repeating the message exactly is the whole contract");
    }

    [Fact]
    public void The_executor_name_is_the_one_a_checkpoint_records()
    {
        EchoReplyExecutorNames.Reply.Should().Be("echo-reply-stage");
    }

    [Fact]
    public async Task The_echo_workflow_served_as_an_agent_answers_with_the_callers_message()
    {
        var agent = new EchoReplyWorkflowFactory()
            .BuildEchoReplyWorkflow()
            .AsAIAgent(name: "echo-agent", includeWorkflowOutputsInResponse: true);

        var response = await agent.RunAsync("hello there");

        response.Text.Should().Contain("hello there");
    }

    [Fact]
    public void The_roster_builds_a_stage_against_a_fake_client()
    {
        // The shared factory is what the host and the tests both use, so this proves the echo profile reaches a
        // real AIAgent with no network and no credential.
        AgentFactory.Build(EchoReplyAgentRoster.EchoReply, new FixedChatClient("{}"))
            .Name.Should().Be("echo-reply");
    }
}
