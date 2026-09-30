using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace CoreRentalNet.Agents.Features.EchoReply;

/// <summary>The echo pipeline, published under the configured name.</summary>
internal sealed class EchoReplyWorkflow(
    EchoReplyWorkflowFactory echoReplyWorkflowFactory,
    EchoAgentIdentity echoAgentIdentity) : IEchoReplyWorkflow
{
    /// <summary>The workflow as one agent, its yielded reply made visible to the caller.</summary>
    public AIAgent AsAIAgent()
        => echoReplyWorkflowFactory.BuildEchoReplyWorkflow()
            .AsAIAgent(
                name: echoAgentIdentity.AgentName,
                includeWorkflowOutputsInResponse: true);
}
