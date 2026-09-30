using Microsoft.Agents.AI;

namespace CoreRentalNet.Agents.Features.EchoReply;

/// <summary>The echo pipeline this deployable serves, published as the one agent the host registers.</summary>
internal interface IEchoReplyWorkflow
{
    /// <summary>The pipeline as one agent, ready for the host to serve.</summary>
    AIAgent AsAIAgent();
}
