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
    private readonly List<string> _stageWords = [];
    private readonly List<string> _narrativeLines = [];
    private WorkspaceSuggestionResultFrame? _outcome;
    private string? _failureCode;
    private bool _hasStopped;

    public bool IsRunning { get; private set; }

    public bool AreStagesCollapsed { get; private set; }

    /// <summary>Whether there is anything to show beyond the field: a run in flight, or a result of one.</summary>
    public bool IsExpanded
        => IsRunning
            || _stageWords.Count > 0
            || _narrativeLines.Count > 0
            || _outcome is not null
            || _failureCode is not null
            || _hasStopped;

    public void BeginSuggestionRun()
    {
        _stageWords.Clear();
        _narrativeLines.Clear();
        _outcome = null;
        _failureCode = null;
        _hasStopped = false;
        AreStagesCollapsed = false;
        IsRunning = true;
    }

    public void AppendStage(string stageWords) => _stageWords.Add(stageWords);

    public void AppendNarrative(string narrativeWords) => _narrativeLines.Add(narrativeWords);

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
    /// Kept and marked not applied rather than retracted mid-read: the text carries no price and no product
    /// name, so leaving it on screen cannot mislead, and a customer who watched a run end needs to know whether
    /// their workspace moved. It did not.
    /// </remarks>
    public IReadOnlyList<SuggestionNotice> BuildNotices()
    {
        var notices = new List<SuggestionNotice>();
        var notApplied = _narrativeLines.Count > 0;

        if (_stageWords.Count > 0)
        {
            notices.Add(new SuggestionStageNotice([.. _stageWords], AreStagesCollapsed && !IsRunning));
        }

        if (_narrativeLines.Count > 0)
        {
            notices.Add(new SuggestionNarrativeNotice([.. _narrativeLines]));
        }

        if (_failureCode is not null)
        {
            notices.Add(new SuggestionFailureNotice(_failureCode, notApplied));
        }

        if (_hasStopped)
        {
            notices.Add(new SuggestionStoppedNotice(notApplied));
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
