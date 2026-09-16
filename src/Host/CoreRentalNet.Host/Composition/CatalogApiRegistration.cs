using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// The catalogue API's JSON: what a machine caller reads, and what it may send back.
/// </summary>
/// <remarks>
/// Enums are written with the camelCase naming policy, so a category arrives as <c>"desk"</c> - the
/// word the caller can send back in the filter. The serializer's default writes the underlying number,
/// which an agent can neither interpret nor return. The OpenAPI document is registered beside this, in
/// <see cref="OpenApiRegistration"/>, so that the description of the endpoint and the shape it
/// actually returns are built from one configuration.
/// </remarks>
internal static class CatalogApiRegistration
{
    public static void AddCatalogApi(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
    }
}
