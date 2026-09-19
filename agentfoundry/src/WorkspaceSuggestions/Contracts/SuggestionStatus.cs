using System.Text.Json.Serialization;

namespace WorkspaceSuggestions.Contracts;

/// <summary>
/// Whether the run produced candidates or the typed not-a-workspace verdict.
/// </summary>
/// <remarks>
/// The verdict is a <b>result</b>, not an error: the application words it and does not report a failure.
/// The serialized names are lower camel case to match <c>suggestion.result.schema.json</c>.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<SuggestionStatus>))]
public enum SuggestionStatus
{
    [JsonStringEnumMemberName("suggested")]
    Suggested,

    [JsonStringEnumMemberName("notWorkspace")]
    NotWorkspace,
}
