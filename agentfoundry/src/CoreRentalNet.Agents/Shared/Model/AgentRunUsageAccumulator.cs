using Microsoft.Extensions.AI;

namespace CoreRentalNet.Agents.Shared.Model;

/// <summary>Adds up what one run's model calls cost, across every stage of it.</summary>
/// <remarks>
/// <para>
/// Scoped to the run, because a cost is the run's and not the process's: the stages of one run share this, and a
/// concurrent run has its own. Nothing here is static, so nothing leaks between callers.
/// </para>
/// <para>
/// An interrupted call still counts as a call — the transport attempted it — but its tokens are whatever the
/// response reported, which for a failure is nothing.
/// </para>
/// </remarks>
public sealed class AgentRunUsageAccumulator(string modelName, string promptVersion)
{
    private int _modelCalls;
    private long _inputTokens;
    private long _outputTokens;

    /// <summary>Records one model call and whatever it reported.</summary>
    public void Record(UsageDetails? usage)
    {
        Interlocked.Increment(ref _modelCalls);

        if (usage is null)
        {
            return;
        }

        Interlocked.Add(ref _inputTokens, usage.InputTokenCount ?? 0);
        Interlocked.Add(ref _outputTokens, usage.OutputTokenCount ?? 0);
    }

    /// <summary>What the run has cost so far.</summary>
    public AgentRunUsage Total => new(
        _modelCalls,
        (int)Interlocked.Read(ref _inputTokens),
        (int)Interlocked.Read(ref _outputTokens),
        modelName,
        promptVersion);
}
