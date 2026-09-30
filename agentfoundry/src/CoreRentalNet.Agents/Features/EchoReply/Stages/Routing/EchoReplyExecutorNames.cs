namespace CoreRentalNet.Agents.Features.EchoReply.Stages.Routing;

/// <summary>The executor name the echo graph is wired by.</summary>
/// <remarks>
/// A name is also what a checkpoint records, so it is a stable string rather than a display label: changing it
/// is a resume-compatibility decision.
/// </remarks>
internal static class EchoReplyExecutorNames
{
    public const string Reply = "echo-reply-stage";
}
