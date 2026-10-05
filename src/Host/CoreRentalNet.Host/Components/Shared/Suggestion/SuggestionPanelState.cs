using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Host.Components.Shared.Suggestion;

/// <summary>The panel's single source of truth: what a run has produced, and how it is to be drawn.</summary>
/// <remarks>
/// The <c>ChatState</c> of this feature. The component mutates nothing on a frame; it calls the frame processor,
/// which calls this, and then renders the notices this builds. Because the state is a plain object the panel
/// creates per instance, a Blazor circuit's shared service scope is not a place a run's state can leak from one
/// customer's panel into another's.
/// </remarks>
internal sealed class SuggestionPanelState
{
    private readonly List<SuggestionStageLine> _stageLines = [];
    private WorkspaceSuggestionResultFrame? _outcome;
    private string? _failureCode;
    private bool _hasStopped;

    public bool IsRunning { get; private set; }

    public bool AreStagesCollapsed { get; private set; }

    /// <summary>Whether there is anything to show beyond the field: a run in flight, or a result of one.</summary>
    public bool IsExpanded
        => IsRunning
            || _stageLines.Count > 0
            || _outcome is not null
            || _failureCode is not null
            || _hasStopped;

    public void BeginSuggestionRun()
    {
        _stageLines.Clear();
        _outcome = null;
        _failureCode = null;
        _hasStopped = false;
        AreStagesCollapsed = false;
        IsRunning = true;
    }

    /// <summary>A stage began: a line that is not done yet, so the last line is where the run is.</summary>
    public void BeginStage(string stageWords)
        => _stageLines.Add(new SuggestionStageLine(stageWords, IsComplete: false));

    /// <summary>A stage finished: the line with those words is done.</summary>
    /// <remarks>
    /// Matched by the words the application gave the stage, which are unique to a stage, so no id travels to the
    /// browser. A completion with no matching line is ignored rather than guessed at.
    /// </remarks>
    public void CompleteStage(string stageWords)
    {
        var index = _stageLines.FindLastIndex(line => line.Words == stageWords);

        if (index >= 0)
        {
            _stageLines[index] = _stageLines[index] with { IsComplete = true };
        }
    }

    /// <summary>A retry or a found setup: over the moment it arrives, so it is born complete.</summary>
    public void AppendCompletedLine(string words)
        => _stageLines.Add(new SuggestionStageLine(words, IsComplete: true));

    public void CompleteWithOutcome(WorkspaceSuggestionResultFrame resultFrame)
    {
        _outcome = resultFrame;
        FinishSuggestionRun();
    }

    public void FailWithCode(string failureCode)
    {
        _failureCode = failureCode;
        FinishSuggestionRun();
    }

    public void MarkStopped()
    {
        _hasStopped = true;
        FinishSuggestionRun();
    }

    public void ExpandStages() => AreStagesCollapsed = false;

    /// <summary>The timeline, in the order the customer read it.</summary>
    /// <remarks>
    /// Retained after a run and collapsed rather than retracted mid-read: where a run went is worth being able to
    /// see afterwards, and worth not having to look at once it is over.
    /// </remarks>
    public IReadOnlyList<SuggestionNotice> BuildNotices()
    {
        var notices = new List<SuggestionNotice>();

        if (_stageLines.Count > 0)
        {
            notices.Add(new SuggestionStageNotice([.. _stageLines], AreStagesCollapsed && !IsRunning));
        }

        if (_failureCode is not null)
        {
            notices.Add(new SuggestionFailureNotice(_failureCode));
        }

        if (_hasStopped)
        {
            notices.Add(new SuggestionStoppedNotice());
        }

        if (_outcome is not null)
        {
            notices.Add(new SuggestionOutcomeNotice(_outcome));
        }

        return notices;
    }

    private void FinishSuggestionRun()
    {
        IsRunning = false;

        // Retained, and collapsed: where a run went is worth being able to see afterwards, and worth not
        // having to look at once it is over.
        AreStagesCollapsed = true;
    }
}
