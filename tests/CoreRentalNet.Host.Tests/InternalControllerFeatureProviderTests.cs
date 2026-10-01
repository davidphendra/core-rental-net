using System.Reflection;
using AwesomeAssertions;
using CoreRentalNet.Host.Controllers;
using CoreRentalNet.Host.Infrastructure;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The rule that lets an internal controller be discovered at all, and the limit of that rule.
/// </summary>
/// <remarks>
/// Asserted against the feature the framework builds its routes from, rather than through a request, because
/// what is being checked is what discovery <b>admits</b>: a test that only fetched the builder's route would
/// pass just as well if every internal class named <c>*Controller</c> had quietly become routable. That the
/// provider is registered, and that the route answers because of it, is what
/// <see cref="CoreRentalNet.Host.Tests.WorkspaceSuggestion.SuggestionEndpointTests"/> covers.
/// </remarks>
public sealed class InternalControllerFeatureProviderTests
{
    [Fact] // non-vacuity: the provider is load-bearing, not decoration
    public void The_framework_s_own_rule_would_not_have_found_an_internal_controller()
    {
        Discovered(new ControllerFeatureProvider()).Should().NotContain(
            typeof(BuilderController).GetTypeInfo(),
            "if the framework's own rule found it, this provider would be doing nothing at all");
    }

    [Fact]
    public void An_internal_controller_that_carries_the_attribute_is_discovered()
    {
        Discovered(new InternalControllerFeatureProvider())
            .Should().Contain(typeof(BuilderController).GetTypeInfo());
    }

    [Fact]
    public void An_internal_class_that_only_names_itself_a_controller_is_not()
    {
        Discovered(new InternalControllerFeatureProvider()).Should().NotContain(
            typeof(UnmarkedProbeController).GetTypeInfo(),
            "the widening is opt-in: an internal type is a controller when it says so, not when it is named so");
    }

    [Fact]
    public void A_public_controller_is_still_discovered_by_the_rule_this_one_extends()
    {
        Discovered(new InternalControllerFeatureProvider())
            .Should().Contain(typeof(CatalogController).GetTypeInfo());
    }

    /// <summary>What one provider admits out of the two assemblies these tests reach into.</summary>
    private static IReadOnlyList<TypeInfo> Discovered(ControllerFeatureProvider provider)
    {
        var feature = new ControllerFeature();

        provider.PopulateFeature(
            [
                new AssemblyPart(typeof(BuilderController).Assembly),
                new AssemblyPart(typeof(UnmarkedProbeController).Assembly),
            ],
            feature);

        return [.. feature.Controllers];
    }
}
