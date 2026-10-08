using AwesomeAssertions;
using Microsoft.Agents.AI.Workflows;
using CoreRentalNet.Agents.Features.EchoReverse;
using CoreRentalNet.Agents.Features.EchoReverse.Routing;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The echo feature end to end: its prompt travels with the binary, its roster builds a stage, and the workflow
/// served as an agent answers with the caller's message.
/// </summary>
public sealed class EchoWorkflowTests
{
    [Fact]
    public void The_executor_name_is_the_one_a_checkpoint_records()
    {
        WorkflowNodeNames.Reverse.Should().Be("echo-reverse");
    }

    [Fact]
    public async Task The_echo_workflow_served_as_an_agent_answers_with_the_callers_message()
    {
        var agent = EchoWorkflowFactory.Build()
            .AsAIAgent(
                name: EchoAgentIdentity.DefaultAgentName,
                includeWorkflowOutputsInResponse: true);

        var response = await agent.RunAsync("hello there");

        response.Text.Should().Contain(new string("hello there".Reverse().ToArray()));
    }
}
