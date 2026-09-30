using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Reading;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Records;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>One run's state: what the handlers mutate, and what the record is read from.</summary>
/// <remarks>
/// The single source of truth for a run. The handlers hold no state of their own, so this is the only thing
/// that has to be passed between them, and the only thing the record is derived from.
/// </remarks>
public sealed class WorkspaceSuggestionRunState(string workspaceQuery)
{
    private readonly WorkspaceSuggestionRunLedger _ledger = new() { Query = workspaceQuery };
    private readonly WorkspaceSuggestionNarrativeFieldReader _reader = new();

    /// <summary>True once the run has reached an ending, so the processor stops reading.</summary>
    public bool HasEnded { get; private set; }

    /// <summary>How the run has ended, or is heading to end. Read to word a run that produced no frame.</summary>
    public WorkspaceSuggestionVerdict Verdict => _ledger.Verdict;

    /// <summary>Keeps the raw text and returns whatever narrative fields have closed since the last fragment.</summary>
    public IReadOnlyList<WorkspaceSuggestionNarrativeField> AppendNarrativeText(string narrativeText)
    {
        _ledger.RawOutput.Append(narrativeText);

        return _reader.Parse(narrativeText);
    }

    /// <summary>The answer, or <c>null</c> when there is nothing in it to show.</summary>
    /// <remarks>
    /// <b>One rule survives here, and it is not a validation.</b> An answer that says it suggested something
    /// and carries no candidate at all is not a suggestion: there is nothing to present, and an empty candidate
    /// list is not a result worth sending. Everything else crosses as the agent stated it, and what makes a
    /// candidate true is the workspace refusing to apply one it cannot honour.
    /// </remarks>
    public WorkspaceSuggestionResultFrame? CompleteWithAgentAnswer(
        WorkspaceSuggestionResultReadyAgentEvent resultReadyAgentEvent)
    {
        var suggestionAnswer = resultReadyAgentEvent.SuggestionAnswer;

        _ledger.RunUsage = suggestionAnswer.RunUsage;
        _ledger.PayloadHash = resultReadyAgentEvent.PayloadHash;
        HasEnded = true;

        if (suggestionAnswer.Status is WorkspaceSuggestionAnswerStatus.NotWorkspace)
        {
            _ledger.Verdict = WorkspaceSuggestionVerdict.Refused;

            return WorkspaceSuggestionResultFrame.Refused();
        }

        if (suggestionAnswer.Status is WorkspaceSuggestionAnswerStatus.CatalogueUnavailable
            && suggestionAnswer.Candidates.Count == 0)
        {
            // The agent streamed its ending without a setup because no catalogue could be searched. That is
            // "unavailable" to the customer, not a malformed answer: nothing was composed from memory, and
            // nothing was wrong with what arrived.
            _ledger.Verdict = WorkspaceSuggestionVerdict.Unavailable;

            return null;
        }

        if (suggestionAnswer.Candidates.Count == 0)
        {
            _ledger.Verdict = WorkspaceSuggestionVerdict.Invalid;

            return null;
        }

        _ledger.Verdict = WorkspaceSuggestionVerdict.Suggested;

        return WorkspaceSuggestionResultFrame.Of(suggestionAnswer.Candidates);
    }

    public void MarkUnavailable()
    {
        _ledger.Verdict = WorkspaceSuggestionVerdict.Unavailable;
        HasEnded = true;
    }

    public void MarkEnded(WorkspaceSuggestionVerdict suggestionVerdict)
        => _ledger.Verdict = suggestionVerdict;

    public WorkspaceSuggestionRunRecord CreateRunRecord(string hashedCustomerIdentity, long startedTimestamp)
        => _ledger.For(hashedCustomerIdentity, startedTimestamp);
}
