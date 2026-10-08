using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using CoreRentalNet.Agents.Features.EchoReverse.Routing;
using CoreRentalNet.Agents.Shared.Workflows;

namespace CoreRentalNet.Agents.Features.EchoReverse.Nodes;

/// <summary>The echo agent's one stage: it asks its stage agent, and publishes what comes back.</summary>
/// <remarks>
/// It derives from the shared chat-entry base, because hosting refuses a workflow whose first stage does not
/// accept the caller's chat messages and a turn token — and because every feature's graph begins the same way.
/// </remarks>
internal sealed class ReverseNodeExecutor()
    : ChatEntryStageExecutor(WorkflowNodeNames.Reverse)
{
    protected override async ValueTask TakeTurnAsync(
        List<ChatMessage> messages,
        IWorkflowContext context,
        bool? emitEvents,
        CancellationToken cancellationToken = default)
    {
        var userMessage = messages.LastOrDefault(message => message.Role == ChatRole.User)?.Text ?? string.Empty;
        var reverseMessage = new string(userMessage?.Reverse().ToArray());

        await WorkflowOutputPublisher.PublishAssistantMessageAsync(
            context,
            reverseMessage,
            cancellationToken);
    }
}
