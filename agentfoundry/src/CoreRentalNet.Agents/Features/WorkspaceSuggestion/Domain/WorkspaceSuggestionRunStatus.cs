using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

/// <summary>How a workspace suggestion run ended, or that it is still going.</summary>
/// <remarks>
/// The three terminal values are the caller's whole vocabulary for an ending. A run is <see cref="Processing" />
/// until one of them is reached, and reaching one is what ends the workflow.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<WorkspaceSuggestionRunStatus>))]
public enum WorkspaceSuggestionRunStatus
{
    [JsonStringEnumMemberName("processing")]
    Processing,

    [JsonStringEnumMemberName("rejected")]
    Rejected,

    [JsonStringEnumMemberName("success")]
    Success,

    [JsonStringEnumMemberName("unavailable")]
    Unavailable,
}
