using System.Text.Json.Serialization;

namespace WorkspaceSuggestions.Contracts;

/// <summary>What the run cost, appended to the answer by the code that made the calls.</summary>
/// <remarks>
/// <para>
/// A separate object rather than a field of <see cref="SuggestionResult"/>, and that is forced by how the
/// answer travels: the model's JSON is streamed verbatim, so by the time anything holds the finished response
/// the text has already gone past. A figure added afterwards cannot be injected into an object that has
/// already been sent, so it follows the answer as its own object instead.
/// </para>
/// <para>
/// The application reads the result where it finds a result and this where it finds a run cost, rather than
/// assuming which came last.
/// </para>
/// </remarks>
public sealed record RunUsageReport(
    [property: JsonPropertyName("runUsage")]
    RunUsage RunUsage);
