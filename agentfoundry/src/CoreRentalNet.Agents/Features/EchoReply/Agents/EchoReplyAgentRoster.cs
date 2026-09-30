using Microsoft.Extensions.AI;
using CoreRentalNet.Agents.Shared.Agents;

namespace CoreRentalNet.Agents.Features.EchoReply.Agents;

/// <summary>The echo agent's one stage, as data, exactly as the workspace pipeline's five are.</summary>
/// <remarks>
/// A stage is a roster entry rather than a subclass, so this feature is a profile, a prompt and a stage — the
/// same three artifacts every other agent here has. The served agent's name (<c>echo-agent</c>) is configured
/// separately: an agent's <see cref="AgentProfile.Name"/> is the stage identity a run records, not the name a
/// console addresses.
/// </remarks>
internal static class EchoReplyAgentRoster
{
    public static AgentProfile EchoReply { get; } = new(
        Name: "echo-reply",
        PromptFileName: "echo-reply.v1.md",
        Description: "Replies with the caller's own message. A test aid: no model, no catalogue, no cost.",
        Output: ChatResponseFormat.Text);

    public static IReadOnlyList<AgentProfile> All { get; } = [EchoReply];
}
