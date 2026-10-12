using System.Text.Json;

namespace CoreRentalNet.CatalogPopulate.Sources;

/// <summary>The one set of JSON options the source files are read with.</summary>
internal static class SourceJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };
}
