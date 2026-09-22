using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Controllers;
using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Microsoft.Extensions.Configuration;

namespace CoreRentalNet.Host.Extentions;

/// <summary>
/// Describes the token the catalogue's endpoints want, so the document says how to obtain one.
/// </summary>
/// <remarks>
/// The scheme is declared only where the deployment has an authority to point at, and a requirement is
/// applied only to a path the policy actually guards: the account routes are not gated, and declaring a token
/// requirement on a login would describe a flow that cannot happen. Each gated path declares the permission
/// its own policy checks - reading the catalogue is not the same permission as searching it by meaning - so a
/// generated client asks for the right one. The flow's scopes are the ones the sign-in already asks for - a
/// permission the provider was never asked for is one it will not issue - while what an endpoint
/// <em>requires</em> is the permission its gate checks. The two endpoint paths are the provider's convention
/// rather than something discovered here: an issuer that published different ones would be read from its
/// discovery document instead.
/// </remarks>
internal sealed class Auth0SecuritySchemeTransformer(
    IdentitySettings identity,
    ClaimSettings claim,
    IConfiguration configuration)
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

        if (document.Paths is not null)
        {
            // Assigned, not appended: a second entry in an operation's list is an alternative, not a stricter
            // rule, so appending would quietly weaken what the endpoint declares it requires.
            Declare(document, CatalogRoutes.Catalogue, claim);
            Declare(
                document,
                CatalogRoutes.CatalogueSimilarity,
                ClaimSettings.From(configuration, ClaimSettings.SimilaritySearch));
        }

        return Task.CompletedTask;
    }

    /// <summary>Gives every operation on one gated path the requirement for that path's permission.</summary>
    private void Declare(OpenApiDocument document, string path, ClaimSettings required)
    {
        if (document.Paths is null
            || !document.Paths.TryGetValue(path, out var item)
            || item.Operations is null)
        {
            return;
        }

        foreach (var operation in item.Operations.Values.OfType<OpenApiOperation>())
        {
            operation.Security = [Requirement(document, required)];
        }
    }

    private OpenApiSecurityRequirement Requirement(OpenApiDocument document, ClaimSettings required)
        => new() { [new OpenApiSecuritySchemeReference(SchemeName, document)] = RequiredScopes(required) };

    /// <summary>The permission the gate checks: the scope this endpoint requires, and nothing else.</summary>
    private static List<string> RequiredScopes(ClaimSettings required)
        => required.IsConfigured && required.ClaimValue is { Length: > 0 } permission ? [permission] : [];

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
