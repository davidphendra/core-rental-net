using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using CoreRentalNet.Agents.Shared.Serialization;

namespace CoreRentalNet.Agents.Shared.Workflows;

/// <summary>Puts one thing on the workflow's output, in the shape the caller reads.</summary>
/// <remarks>
/// <para>
/// A workflow event does not cross the agent boundary as content; a yielded message does. Output is therefore
/// yielded as an assistant message, and the host turns each yielded message into one response delta — which is
/// what makes a caller's stream a sequence of typed events rather than a transcript of the model.
/// </para>
/// <para>
/// <b>A parameter of a union's base type is not decoration.</b> The polymorphic discriminator is written only
/// when the value is serialized as its base, so a publisher typed <c>object</c> — or one called with a
/// concrete-typed variable — silently drops the <c>type</c> field and every caller stops being able to tell one
/// event from another. The stage bases fix the union as a class type parameter, which is what makes that
/// mistake unavailable at a call site.
/// </para>
/// </remarks>
internal static class WorkflowOutputPublisher
{
    /// <summary>One typed event, published through the feature's union base.</summary>
    public static ValueTask PublishStreamEventAsync<TStreamEvent>(
        IWorkflowContext workflowContext,
        TStreamEvent streamEvent,
        CancellationToken cancellationToken)
        where TStreamEvent : class
        => workflowContext.YieldOutputAsync(
            new ChatMessage(ChatRole.Assistant, ContractJson.Serialize(streamEvent)),
            cancellationToken);

    /// <summary>One plain assistant message, for a stage that answers in words rather than in events.</summary>
    public static ValueTask PublishAssistantMessageAsync(
        IWorkflowContext workflowContext,
        string text,
        CancellationToken cancellationToken)
        => workflowContext.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, text), cancellationToken);
}
