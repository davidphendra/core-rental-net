using System.Text.Json;

namespace CoreRentalNet.CatalogPopulate.Tests;

/// <summary>A detached <see cref="JsonElement"/> for building a details dictionary in a test.</summary>
internal static class JsonValue
{
    public static JsonElement Of(string raw)
    {
        using var document = JsonDocument.Parse(raw);

        return document.RootElement.Clone();
    }
}
