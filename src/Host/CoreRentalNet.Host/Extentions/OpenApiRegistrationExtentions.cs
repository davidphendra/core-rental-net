namespace CoreRentalNet.Host.Extentions;

/// <summary>
/// The machine-readable description of the API, and the security it advertises.
/// </summary>
/// <remarks>
/// Registered always so the document is ready wherever it is mapped; the mapping itself is
/// development only, in <see cref="ApplicationPipelineExtentions"/>.
/// </remarks>
internal static class OpenApiRegistrationExtentions
{
    public static void AddCatalogOpenApi(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<Auth0SecuritySchemeTransformer>();
            options.AddOperationTransformer<CatalogQueryParameterTransformer>();
        });
    }
}
