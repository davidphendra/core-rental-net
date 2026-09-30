using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using CoreRentalNet.Agents.Features.EchoReply.Stages.Routing;
using CoreRentalNet.Agents.Shared.Workflows;

namespace CoreRentalNet.Agents.Features.EchoReply.Stages;

/// <summary>The echo agent's one stage: it asks its stage agent, and publishes what comes back.</summary>
/// <remarks>
/// It derives from the shared chat-entry base, because hosting refuses a workflow whose first stage does not
/// accept the caller's chat messages and a turn token — and because every feature's graph begins the same way.
/// </remarks>
internal sealed class EchoReplyStageExecutor(AIAgent stageAgent)
    : ChatEntryStageExecutor(EchoReplyExecutorNames.Reply)
{
    protected override async ValueTask TakeTurnAsync(
        List<ChatMessage> messages,
        IWorkflowContext context,
        bool? emitEvents,
        CancellationToken cancellationToken = default)
    {
        var agentResponse = await stageAgent.RunAsync(messages, cancellationToken: cancellationToken);

        await WorkflowOutputPublisher.PublishAssistantMessageAsync(
            context, agentResponse.Text ?? string.Empty, cancellationToken);
    }
}
