using CoreRentalNet.Host.Controllers;
using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// Describes the token the catalogue endpoint wants, so the document says how to obtain one.
/// </summary>
/// <remarks>
/// The scheme is declared only where the deployment has an authority to point at, and the requirement
/// is applied only to the endpoint the policy actually guards: the account routes are not gated, and
/// declaring a token requirement on a login would describe a flow that cannot happen. The flow's scopes
/// are the ones the sign-in already asks for - a permission the provider was never asked for is one it
/// will not issue - while what the endpoint <em>requires</em> is the one permission the gate checks.
/// The two endpoint paths are the provider's convention rather than something discovered here: an
/// issuer that published different ones would be read from its discovery document instead.
/// </remarks>
internal sealed class Auth0SecuritySchemeTransformer(IdentitySettings identity, ClaimSettings claim)
    : IOpenApiDocumentTransformer
{
    public const string SchemeName = "Auth0";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (Authority is not { } authority)
        {
            return Task.CompletedTask;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{authority}/authorize"),
                    TokenUrl = new Uri($"{authority}/oauth/token"),
                    Scopes = DescribeFlowScopes(),
                },
            },
        };

        if (document.Paths is null
            || !document.Paths.TryGetValue(CatalogRoutes.Catalogue, out var path)
            || path.Operations is null)
        {
            return Task.CompletedTask;
        }

        foreach (var operation in path.Operations.Values.OfType<OpenApiOperation>())
        {
            // Assigned, not appended: a second entry in this list is an alternative, not a stricter
            // rule, so appending would quietly weaken what the endpoint declares it requires.
            operation.Security = new List<OpenApiSecurityRequirement> { Requirement(document) };
        }

        return Task.CompletedTask;
    }

    private OpenApiSecurityRequirement Requirement(OpenApiDocument document)
        => new() { [new OpenApiSecuritySchemeReference(SchemeName, document)] = RequiredScopes() };

    /// <summary>The permission the gate checks: the scope this endpoint requires, and nothing else.</summary>
    private List<string> RequiredScopes()
        => claim.IsConfigured && claim.ClaimValue is { Length: > 0 } permission ? [permission] : [];

    private Dictionary<string, string> DescribeFlowScopes()
        => identity.Scope
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToDictionary(scope => scope, Describe);

    /// <summary>A description per scope: the document is read by people, and the specification asks for one.</summary>
    /// </summary>
    /// <remarks>
    /// The permission scopes are described generically on purpose. Naming the one this API requires
    /// would write the configured claim into the code, which an architecture test forbids: the claim is
    /// configuration, and the transformer already takes the required scope from it.
    /// </remarks>
    private static string Describe(string scope) => scope switch
    {
        "openid" => "Identify the signed-in person.",
        "profile" => "Read the signed-in person's name.",
        "email" => "Read the signed-in person's email address.",
        _ => "A permission the provider issues for this API.",
    };

    /// <summary>The provider's issuer, from the same override the sign-in handler uses when it is set.</summary>
    private string? Authority => identity.Authority
        ?? (identity.Domain is { Length: > 0 } domain ? $"https://{domain}" : null);
}
