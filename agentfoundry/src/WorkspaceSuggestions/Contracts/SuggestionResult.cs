using System.ComponentModel;
using System.Text.Json.Serialization;

namespace WorkspaceSuggestions.Contracts;

/// <summary>What the agent answers with: candidates, or the typed not-a-workspace verdict.</summary>
/// <remarks>
/// <para>
/// This is the wire shape of <c>suggestion.result.schema.json</c> and the structured-output type the agent is
/// configured with. <b>The name and the amount on each line are the catalogue tool's</b> - the model states
/// what it was given rather than inventing it - and the application sums the lines for the candidate's
/// total. No image crosses, and no band label is carried.
/// </para>
/// <para>
/// <b>It carries no usage, deliberately.</b> The run's cost was once a required field of this type, which
/// meant the only party asked for a model-call count, a tokenService count, a deployment name and a prompt version
/// was the one party unable to observe any of them: a schema demanding <c>two</c> from a model that cannot
/// count its own calls, and validation would have passed because integers are integers. The framework counts
/// those, so the code reports them — see <see cref="RunUsageReport"/> — and the model is asked only what it
/// knows: what the customer wants, and which catalogue items satisfy it.
/// </para>
/// </remarks>
public sealed record SuggestionResult(
    [property: JsonPropertyName("status")]
    [property: Description("Whether the run produced candidates or refused the request as not about a workspace.")]
    SuggestionStatus Status,
    [property: JsonPropertyName("reason")]
    [property: Description("A one-line reason, present when the status is notWorkspace.")]
    string? Reason,
    [property: JsonPropertyName("options")]
    [property: Description("Up to three genuinely different setups. Fewer is fewer — never padded to reach three.")]
    IReadOnlyList<SuggestionOption> Options);
