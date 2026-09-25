using System.Text.Json;
using System.Text.Json.Serialization;
using CoreRentalNet.Host.Binders;
using CoreRentalNet.Host.Infrastructure;

namespace CoreRentalNet.Host.Extentions;

/// <summary>
/// The catalogue API's surface: the controller pipeline, and the JSON it reads and writes.
/// </summary>
/// <remarks>
/// <para>
/// The filter vocabulary is bound by the application's own binder
/// (<see cref="CatalogEnumBinderProvider"/>), so a filter that is not one of the catalogue's words
/// becomes a model-state error and <c>[ApiController]</c> answers it with a <c>400</c> the action
/// never sees.
/// </para>
/// <para>
/// Enums are written with the camelCase naming policy, so a category arrives as <c>"desk"</c> - the
/// word the caller can send back in the filter. The serializer's default writes the underlying number,
/// which an agent can neither interpret nor return. Controllers and endpoint mappings each hold their
/// own serializer options, so the same configuration is applied to both: two copies is how the
/// document, the endpoint and the machine caller come to disagree about one value.
/// </para>
/// </remarks>
internal static class CatalogApiRegistrationExtentions
{
    public static void AddCatalogApi(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services
            .AddControllers(options => options.ModelBinderProviders.Insert(0, new CatalogEnumBinderProvider()))
            .AddJsonOptions(options => Configure(options.JsonSerializerOptions))
            // The framework admits a public controller only, and the builder's controller is internal because a
            // public type may not name the guard, the agent or the validator. Added beside the framework's own
            // provider rather than in place of it, so the rule for public controllers is untouched; what this
            // adds is the provider's own rule, which is opt-in. See the provider for the why.
            .ConfigureApplicationPartManager(manager =>
                manager.FeatureProviders.Add(new InternalControllerFeatureProvider()));

        builder.Services.ConfigureHttpJsonOptions(options => Configure(options.SerializerOptions));
    }

    private static void Configure(JsonSerializerOptions options)
        => options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
}
