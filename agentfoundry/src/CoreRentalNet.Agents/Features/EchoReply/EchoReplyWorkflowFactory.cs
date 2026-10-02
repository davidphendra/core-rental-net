using Microsoft.Agents.AI.Workflows;
using CoreRentalNet.Agents.Features.EchoReply.Agents;
using CoreRentalNet.Agents.Features.EchoReply.ChatClient;
using CoreRentalNet.Agents.Features.EchoReply.Stages;
using CoreRentalNet.Agents.Shared.Agents;
using CoreRentalNet.Agents.Shared.ChatClients;

namespace CoreRentalNet.Agents.Features.EchoReply;

/// <summary>Builds the echo graph: one stage, served as one agent.</summary>
/// <remarks>
/// It is a workflow with a single stage rather than a bare chat agent on purpose. Hosting treats an agent that
/// runs a workflow differently from one that does not — it applies checkpointing and it probes the workflow's
/// checkpoints on readiness — so a plain agent would exercise a different hosting path from the one the real
/// pipeline uses, and would be a weaker test aid for exactly that reason.
/// </remarks>
internal sealed class EchoReplyWorkflowFactory
{
    public Workflow BuildEchoReplyWorkflow()
    {
        var echoReplyStage = new EchoReplyStageExecutor(
            AgentFactory.Build(EchoReplyAgentRoster.EchoReply, new EchoReplyChatClient(NoOpChatClient.Instance))).BindExecutor();

        return new WorkflowBuilder(echoReplyStage)
            .WithOutputFrom(echoReplyStage)
            .WithName("echo-reply-workflow")
            .Build();
    }
}
