namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>Every MCP tool answer of one attempt, recorded as it returns.</summary>
/// <remarks>
/// <para>
/// <b>Scoped like a run, and cleared when retrieval begins.</b> A run makes one retrieval per attempt, and each
/// attempt's searches are the only ones its reranker may consider — an accumulating ledger would hand attempt two
/// the products attempt one rejected, which is the loop the retry edge exists to break.
/// </para>
/// <para>
/// <b>Generic on purpose.</b> It records that a tool was called and what it said, and knows nothing about
/// catalogues; projecting those answers into workspace products is the feature's job. Shared code learning a
/// feature's vocabulary is how a shared folder stops being shared.
/// </para>
/// <para>
/// A single turn may call several tools at once, so recording is serialised rather than assumed to be
/// single-threaded.
/// </para>
/// </remarks>
public sealed class McpToolAnswerLedger
{
    private readonly Lock _recordedAnswersLock = new();
    private readonly List<RecordedMcpToolAnswer> _recordedAnswers = [];

    /// <summary>What the tools have answered since the ledger was last cleared.</summary>
    public IReadOnlyList<RecordedMcpToolAnswer> RecordedAnswers
    {
        get
        {
            lock (_recordedAnswersLock)
            {
                return [.. _recordedAnswers];
            }
        }
    }

    /// <summary>Records one answer, before it is handed back to the model that asked for it.</summary>
    public void Record(string toolName, IReadOnlyDictionary<string, object?> toolArguments, string answerText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentNullException.ThrowIfNull(toolArguments);
        ArgumentNullException.ThrowIfNull(answerText);

        lock (_recordedAnswersLock)
        {
            _recordedAnswers.Add(
                new RecordedMcpToolAnswer(toolName, new Dictionary<string, object?>(toolArguments), answerText));
        }
    }

    /// <summary>Forgets every answer, which is what starting an attempt's retrieval means.</summary>
    public void ClearRecordedAnswers()
    {
        lock (_recordedAnswersLock)
        {
            _recordedAnswers.Clear();
        }
    }
}
