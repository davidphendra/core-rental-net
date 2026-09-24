using System.Diagnostics;
using System.Text;
using CoreRentalNet.Host.Agents;

namespace CoreRentalNet.Host.AiBuilder;

/// <summary>What a run accumulates while it happens, and the record it becomes when it ends.</summary>
/// <remarks>
/// <para>
/// <b>The record is assembled from what was observed rather than from what was hoped.</b> The query is the one
/// the run was given, the raw output is what the model wrote, and the verdict is the one the run reached.
/// Nothing here is derived from a second reading of anything.
/// </para>
/// <para>
/// It is kept apart from the run itself because a run does several things and the record is one of them: the
/// query, the model's answer and the ending each write to this, and none of them has to know how the record is
/// shaped.
/// </para>
/// </remarks>
internal sealed class RunLedger
{
    /// <summary>The customer's own words, trimmed as the request builder trims them.</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>The hash of the projection actually sent.</summary>
    public string PayloadHash { get; set; } = string.Empty;

    /// <summary>What the run cost, as the agent reported it.</summary>
    public AgentRunUsage? Usage { get; set; }

    /// <summary>How the run ended, which the record writes as a word.</summary>
    public AiRunVerdict Verdict { get; set; } = AiRunVerdict.Unavailable;

    /// <summary>Everything the model wrote, kept because a rejected run is otherwise unexplainable.</summary>
    public StringBuilder Raw { get; } = new();

    /// <summary>The record, once there is nothing left to wait for.</summary>
    public AiRunRecord For(string customer, long started)
        => new(
            RunId: Guid.NewGuid().ToString("n"),
            Query: Query,
            PayloadHash: PayloadHash,
            Model: Usage?.Model ?? string.Empty,
            PromptVersion: Usage?.PromptVersion ?? string.Empty,
            ModelCalls: Usage?.ModelCalls ?? 0,
            InputTokens: Usage?.InputTokens ?? 0,
            OutputTokens: Usage?.OutputTokens ?? 0,
            LatencyMilliseconds: (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            RawOutput: Raw.ToString(),
            Verdict: Verdict,
            CustomerId: customer);
}
