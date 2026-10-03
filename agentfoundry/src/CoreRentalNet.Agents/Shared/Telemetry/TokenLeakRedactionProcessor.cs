using System.Diagnostics;
using OpenTelemetry;

namespace CoreRentalNet.Agents.Shared.Telemetry;

/// <summary>Removes generative content from a span the guardrail marked as a token leak, before export.</summary>
/// <remarks>
/// <para>
/// Content capture means a model that echoed the caller's token would place it on the chat span before the
/// guardrail throws - and a throw does not retract an attribute already recorded. The guardrail sets the mark at
/// throw time; this processor scrubs the generative content of any marked span on the way out, so the promise that
/// the token never leaves holds for telemetry as well as for the customer's answer.
/// </para>
/// <para>
/// It is a processor rather than a decorator because it must run after every decorator, at export, which is the
/// only point at which a span is final.
/// </para>
/// </remarks>
internal sealed class TokenLeakRedactionProcessor : BaseProcessor<Activity>
{
    private const string Redacted = "[redacted: token leak]";

    /// <summary>The span a guardrail stopped is scrubbed; every other span passes through untouched.</summary>
    public override void OnEnd(Activity activity)
    {
        if (activity.GetTagItem(WorkspaceTelemetry.TokenLeakDetected) is not true)
        {
            return;
        }

        WorkspaceTelemetry.TelemetryRedaction.Add(1, new System.Diagnostics.TagList
        {
            { WorkspaceTelemetry.Reason, "token_leak" },
        });

        foreach (var tag in activity.TagObjects.ToList())
        {
            if (tag.Key.StartsWith("gen_ai.", StringComparison.Ordinal))
            {
                activity.SetTag(tag.Key, Redacted);
            }
        }
    }
}
