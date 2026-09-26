using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

namespace CoreRentalNet.Host.Components.Shared.Suggestion;

/// <summary>Reads the terminal frame a run writes, and never guesses at one it cannot read.</summary>
/// <remarks>
/// The single place that knows both the wire's <c>result</c> frame and the panel's model, so a frame whose
/// shape changes is a change here rather than a change in the component.
/// </remarks>
internal static class SuggestionNoticeReader
{
    public static WorkspaceSuggestionResultFrame? ReadOutcomeFrame(string resultJson)
        => WorkspaceSuggestionResultJson.Deserialize(resultJson);
}
