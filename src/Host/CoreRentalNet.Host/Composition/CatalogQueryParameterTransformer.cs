using CoreRentalNet.Host.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// Describes the query parameters the framework cannot describe by itself.
/// </summary>
/// <remarks>
/// <para>
/// Descriptions do not otherwise reach the document, because this solution builds with
/// <c>GenerateDocumentationFile</c> off.
/// </para>
/// <para>
/// <c>view</c> needs one and cannot be a schema: a status code has one schema per content type, so
/// declaring a second <c>200</c> for the compact answer replaces the schema of the first and the
/// document would describe the projection while claiming it is the default. Without this a caller
/// reading the document sees a parameter with no values and no meaning.
/// </para>
/// <para>
/// <c>limit</c> needs one because its absence has a meaning a caller cannot guess: no limit means the
/// endpoint's own default, not "everything", and the answer says which it was.
/// </para>
/// </remarks>
internal sealed class CatalogQueryParameterTransformer : IOpenApiOperationTransformer
{
    /// <summary>The projection parameter, as the action names it.</summary>
    public const string View = "view";

    /// <summary>The cap parameter, as the action names it.</summary>
    public const string Limit = "limit";

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
            parameter.Description = parameter.Name switch
            {
                View => DescribeView(),
                Limit => DescribeLimit(),
                _ => parameter.Description,
            };
        }

        return Task.CompletedTask;
    }

    /// <summary>Both projections, and what each one leaves out, in the words the caller needs.</summary>
    private static string DescribeView()
        => "How much of each product to return. 'full' is every published field and is what a caller "
            + "that says nothing gets; 'compact' leaves out the image path and the display flags, states "
            + "the price as a number with the currency once on the envelope, and keeps the description "
            + "and the metadata.";

    /// <summary>The cap, its default, and what the answer does about it.</summary>
    private static string DescribeLimit()
        => $"How many products to return. Left out, the endpoint returns up to {CatalogApiLimits.Default}. "
            + "The answer states how many products matched beside how many it carries, so a caller can "
            + "tell a complete answer from a partial one.";
}
