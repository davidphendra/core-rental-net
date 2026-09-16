namespace CoreRentalNet.Host.Composition;

/// <summary>
/// The machine-readable description of the API, and the security it advertises.
/// </summary>
/// <remarks>
/// Registered always so the document is ready wherever it is mapped; the mapping itself is
/// development only, in <see cref="ApplicationPipeline"/>.
/// </remarks>
internal static class OpenApiRegistration
{
    public static void AddCatalogOpenApi(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOpenApi(options =>
            options.AddDocumentTransformer<Auth0SecuritySchemeTransformer>());
    }
}
