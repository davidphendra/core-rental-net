using System.Text.Json.Serialization;

namespace WorkspaceSuggestions.Contracts;

/// <summary>Whether the rephraser understood a workspace request, or refused it as something else.</summary>
/// <remarks>
/// The refusal is a <b>result</b>, not an error: the application words it and reports no failure. The
/// serialized names match <c>workspace-spec.schema.json</c>.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<SpecificationStatus>))]
public enum SpecificationStatus
{
    [JsonStringEnumMemberName("spec")]
    Spec,

    [JsonStringEnumMemberName("notWorkspace")]
    NotWorkspace,
}
