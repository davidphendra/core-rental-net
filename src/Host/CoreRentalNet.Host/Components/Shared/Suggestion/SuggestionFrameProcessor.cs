using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

namespace CoreRentalNet.Host.Components.Shared.Suggestion;

/// <summary>Applies one server-sent frame to the panel state, so the component accumulates nothing.</summary>
/// <remarks>
/// The browser-side processor: the component hands it each frame and renders whatever state it leaves, which is
/// what keeps a run's state out of the markup and in one testable place.
/// </remarks>
internal sealed class SuggestionFrameProcessor(SuggestionPanelState suggestionPanelState)
{
    /// <summary>Applies the frame, and returns the new live-region announcement or null when there is none.</summary>
    public string? ProcessServerSentEvent(string serverSentEventName, string serverSentEventData)
    {
        switch (serverSentEventName)
        {
            case "stage":
                // The stage lines are what a screen reader hears while a run is going, because they are short
                // and the application wrote them. The prose never goes through here.
                suggestionPanelState.AppendStage(serverSentEventData);

                return serverSentEventData;

            case "text":
                suggestionPanelState.AppendNarrative(serverSentEventData);

                return null;

            case "failed":
                // A code, not a sentence: the words belong here, and the agent's own reason is diagnostic.
                suggestionPanelState.FailWithCode(serverSentEventData);

                return null;

            case "result":
                return ApplyOutcome(serverSentEventData);

            default:
                return null;
        }
    }

    /// <summary>The candidates ARE the terminal outcome, so this says they arrived rather than reading a line.</summary>
    private string? ApplyOutcome(string resultJson)
    {
        if (SuggestionNoticeReader.ReadOutcomeFrame(resultJson) is { } resultFrame)
        {
            suggestionPanelState.CompleteWithOutcome(resultFrame);

            return resultFrame.Status is WorkspaceSuggestionResultFrame.NotWorkspace
                ? "That doesn't look like a workspace request."
                : "Your workspace suggestions are ready.";
        }

        suggestionPanelState.FailWithCode(WorkspaceSuggestionFailureCode.Invalid);

        return null;
    }
}
