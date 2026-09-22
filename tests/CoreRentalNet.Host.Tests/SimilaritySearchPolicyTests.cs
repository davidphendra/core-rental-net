using AwesomeAssertions;
using CoreRentalNet.Host.Extentions;
using CoreRentalNet.Host.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The similarityService search's policy as the application actually wires it: a requirement of its own, on the bearer
/// scheme where there is a provider, and closed where there is not.
/// </summary>
/// <remarks>
/// Built through the registrations rather than by hand, so the test fails if the composition root stops
/// registering the policy or the scheme. It is a second policy rather than a reuse of the catalogue's, which is
/// the property that lets a deployment allow reading and not searching.
/// </remarks>
public sealed class SimilaritySearchPolicyTests
{
    [Fact]
    public void With_a_provider_the_policy_requires_its_claim_on_the_bearer_scheme()
    {
        using var services = Composition(withProvider: true);

        var policy = Policy(services);

        policy.Requirements.Should().ContainSingle().Which.Should().BeOfType<ClaimRequirement>();
        policy.AuthenticationSchemes.Should().ContainSingle().Which.Should().Be(JwtBearerDefaults.AuthenticationScheme);
    }

    [Fact]
    public void With_no_provider_the_policy_names_no_scheme()
    {
        // Nothing is registered to authenticate against, and a policy that names a scheme nobody registered
        // throws when authorization runs. The requirement closes the endpoint by itself.
        using var services = Composition(withProvider: false);

        Policy(services).AuthenticationSchemes.Should().BeEmpty();
    }

    [Fact]
    public void The_similarity_policy_is_not_the_catalogue_policy()
    {
        SimilaritySearchPolicy.Name.Should().NotBe(CatalogApiPolicy.Name);
        SimilaritySearchPolicy.Name.Should().NotBe(CatalogPolicy.Name);
    }

    private static AuthorizationPolicy Policy(IServiceProvider services)
        => services.GetRequiredService<IOptions<AuthorizationOptions>>().Value.GetPolicy(SimilaritySearchPolicy.Name)!;

    private static ServiceProvider Composition(bool withProvider)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Auth0:Enabled"] = withProvider ? "true" : "false",
            ["Auth0:Domain"] = withProvider ? "tenant.example" : null,
            ["Auth0:ClientId"] = withProvider ? "client" : null,
            ["Auth0:ClientSecret"] = withProvider ? "secret" : null,
            ["Authorization:SimilaritySearch:ClaimType"] = "permissions",
            ["Authorization:SimilaritySearch:ClaimValue"] = "searchsimilarity:aibuilder",
        };

        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(settings);

        var identity = builder.AddOptionalIdentity();
        builder.AddCatalogApiAuthentication(identity);
        builder.AddSimilaritySearchAuthorization(identity);

        return builder.Services.BuildServiceProvider();
    }
}
