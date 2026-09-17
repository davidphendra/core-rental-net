using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// Describes the projection a caller may ask for, because the document cannot show two of them.
/// </summary>
/// <remarks>
/// A status code has one schema per content type, so declaring a second <c>200</c> for the compact
/// answer replaces the schema of the first and the document would describe the projection while
/// claiming it is the default. The vocabulary is therefore carried by the parameter itself: without
/// this a caller reading the document sees a <c>view</c> parameter with no values and no meaning, and
/// the projection is undiscoverable. Descriptions do not otherwise reach the document, because this
/// solution builds with <c>GenerateDocumentationFile</c> off.
/// </remarks>
internal sealed class CatalogProjectionDescriptionTransformer : IOpenApiOperationTransformer
{
    /// <summary>The parameter this describes, as the action names it.</summary>
    public const string Parameter = "view";

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (operation.Parameters is null)
        {
            return Task.CompletedTask;
        }

        foreach (var parameter in operation.Parameters)
        {
            if (parameter.Name == Parameter)
            {
                parameter.Description = Describe();
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>Both values, and what each one leaves out, in the words the caller needs.</summary>
    private static string Describe()
        => "How much of each product to return. 'full' is every published field and is what a caller "
            + "that says nothing gets; 'compact' leaves out the image path and the display flags, states "
            + "the price as a number with the currency once on the envelope, and keeps the description "
            + "and the metadata.";
}
