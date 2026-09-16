using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// Describes the token the catalogue endpoint wants, so the document says how to obtain one.
/// </summary>
/// <remarks>
/// Everything here comes from the identity configuration rather than from a second copy of it: the
/// provider's authority is where its authorization and token endpoints live, and the scopes are the
/// ones the login asks for - a scope named here that the provider was never asked for is a permission
/// it will not issue. With no provider configured there is nothing to describe, and the document is
/// left as it is.
/// </remarks>
internal sealed class Auth0SecuritySchemeTransformer(IdentitySettings identity) : IOpenApiDocumentTransformer
{
    /// <summary>The scheme's name, which is how an operation points at it.</summary>
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

        var scopes = Scopes();

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
                    Scopes = scopes,
                },
            },
        };

        if (document.Paths is null)
        {
            return Task.CompletedTask;
        }

        foreach (var path in document.Paths.Values)
        {
            if (path.Operations is null)
            {
                continue;
            }

            foreach (var operation in path.Operations.Values.OfType<OpenApiOperation>())
            {
                operation.Security ??= [];
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(SchemeName, document)] = [.. scopes.Keys],
                });
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>The provider's issuer, from the same override the sign-in handler uses when it is set.</summary>
    private string? Authority => identity.Authority
        ?? (identity.Domain is { Length: > 0 } domain ? $"https://{domain}" : null);

    private Dictionary<string, string> Scopes()
        => identity.Scope
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToDictionary(scope => scope, _ => string.Empty);
}
