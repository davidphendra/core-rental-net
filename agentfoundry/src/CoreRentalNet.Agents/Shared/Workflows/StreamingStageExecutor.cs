using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Agents.Shared.Workflows;

/// <summary>A stage of any workflow whose state travels the edges, able to publish its feature's events.</summary>
/// <remarks>
/// The workflow state travels the edges by the framework's own auto-send, and is declared here so the protocol
/// accepts it. The feature's event union is a class type parameter rather than a method type parameter, so a
/// stage cannot accidentally publish through a concrete type and lose the discriminator.
/// </remarks>
internal abstract class StreamingStageExecutor<TWorkflowState, TStreamEvent>(string executorName)
    : Executor<TWorkflowState, TWorkflowState>(executorName)
    where TStreamEvent : class
{
    /// <summary>Declares the event message, because the runtime validates a yield against the protocol.</summary>
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
        => base.ConfigureProtocol(protocolBuilder).YieldsOutput<ChatMessage>();

    /// <summary>Publishes one of this feature's events, through its union base.</summary>
    protected static ValueTask PublishStreamEventAsync(
        IWorkflowContext workflowContext,
        TStreamEvent streamEvent,
        CancellationToken cancellationToken)
        => WorkflowOutputPublisher.PublishStreamEventAsync(workflowContext, streamEvent, cancellationToken);
}
