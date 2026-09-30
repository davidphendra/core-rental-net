using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Agents.Shared.Workflows;

/// <summary>The stage a workflow begins at, which must speak the chat protocol hosting requires.</summary>
/// <remarks>
/// <para>
/// Hosting serves a workflow as an agent only when the workflow accepts the caller's chat messages <b>and</b> a
/// turn token, and it says so before a run starts: <c>Workflow does not support ChatProtocol: At least
/// List&lt;ChatMessage&gt; and TurnToken must be supported as input.</c> That is why the first stage of every
/// feature's graph derives from here rather than from a typed executor.
/// </para>
/// <para>
/// It declares the assistant message as a yieldable output, which is what lets a feature publish events or a
/// plain reply from its entry stage.
/// </para>
/// </remarks>
internal abstract class ChatEntryStageExecutor(string executorName) : ChatProtocolExecutor(executorName)
{
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
        => base.ConfigureProtocol(protocolBuilder).YieldsOutput<ChatMessage>();
}
