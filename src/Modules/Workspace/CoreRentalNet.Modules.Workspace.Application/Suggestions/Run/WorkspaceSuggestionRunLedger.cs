using System.Diagnostics;
using System.Text;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Records;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

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
internal sealed class WorkspaceSuggestionRunLedger
{
    /// <summary>The customer's own words, trimmed as the request factory trims them.</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>The hash of the payload actually sent.</summary>
    public string PayloadHash { get; set; } = string.Empty;

    /// <summary>What the run cost, as the agent reported it.</summary>
    public WorkspaceSuggestionRunUsage? RunUsage { get; set; }

    /// <summary>How the run ended, which the record writes as a word.</summary>
    public WorkspaceSuggestionVerdict Verdict { get; set; } = WorkspaceSuggestionVerdict.Unavailable;

    /// <summary>Everything the model wrote, kept because a rejected run is otherwise unexplainable.</summary>
    public StringBuilder RawOutput { get; } = new();

    /// <summary>The record, once there is nothing left to wait for.</summary>
    public WorkspaceSuggestionRunRecord For(string hashedCustomerIdentity, long startedTimestamp)
        => new(
            RunId: Guid.NewGuid().ToString("n"),
            Query: Query,
            PayloadHash: PayloadHash,
            Model: RunUsage?.Model ?? string.Empty,
            PromptVersion: RunUsage?.PromptVersion ?? string.Empty,
            ModelCalls: RunUsage?.ModelCalls ?? 0,
            InputTokens: RunUsage?.InputTokens ?? 0,
            OutputTokens: RunUsage?.OutputTokens ?? 0,
            LatencyMilliseconds: (long)Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds,
            RawOutput: RawOutput.ToString(),
            Verdict: Verdict,
            CustomerId: hashedCustomerIdentity);
}
