using System.Text.Json;
using System.Text.Json.Serialization;
using CoreRentalNet.Agents.Shared.Serialization;

namespace CoreRentalNet.Agents.Shared.Serialization;

/// <summary>The one serializer configuration for the contracts, on the wire and in the run record.</summary>
/// <remarks>
/// <para>
/// The same conventions the schemas declare: camel-case property names, and enums as the words the schema lists
/// rather than as numbers. Having one place means a report the code writes and an answer the model writes
/// cannot end up spelled two ways.
/// </para>
/// <para>
/// <b>Enum words come from each enum, not from a naming policy here.</b> A camel-case converter added to these
/// options applies to every enum and overrides the converter an enum carries, which turns the slot vocabulary's
/// PascalCase ids into camel case and breaks the contract on the way to the caller. Each enum therefore states
/// its own wire words, and this configuration states none.
/// </para>
/// </remarks>
internal static class ContractJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    private static JsonSerializerOptions Create()
    {
        // Deliberately no enum converter: one here would override every enum's own converter, and the slot
        // vocabulary is PascalCase by contract.
        return new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }
}
