using System.Text.Json;
using Json.Schema;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The committed schemas, parsed once, and a way to check an instance against one.</summary>
/// <remarks>
/// Parsed once because <c>JsonSchema.FromText</c> registers a schema globally by its <c>$id</c>, and
/// registering the same one twice throws "Overwriting registered schemas is not permitted" — so parsing per
/// test would fail from the second test onwards. The files are the ones the application pins its DTOs to,
/// copied to the output by the project file.
/// </remarks>
internal static class Schemas
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static string Directory => Path.Combine(AppContext.BaseDirectory, "contracts");

    private static readonly Lazy<IReadOnlyDictionary<string, JsonSchema>> Parsed = new(() =>
        System.IO.Directory.GetFiles(Directory, "*.json")
            .ToDictionary(
                file => Path.GetFileName(file)!,
                file => JsonSchema.FromText(File.ReadAllText(file))!));

    public static JsonSchema Get(string file) => Parsed.Value[file];

    public static EvaluationResults Evaluate(string file, object instance)
        => Get(file).Evaluate(JsonSerializer.SerializeToElement(instance, Json));

    public static EvaluationResults EvaluateRaw(string file, string json)
        => Get(file).Evaluate(JsonDocument.Parse(json).RootElement);
}
