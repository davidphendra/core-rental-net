using AwesomeAssertions;
using CoreRentalNet.Host.Infrastructure;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The address that starts a sign-in, and the return address it carries.
/// </summary>
public sealed class SignInUrlTests
{
    [Fact] // AUTH-10
    public void A_local_return_address_is_carried_through_escaped()
        => SignInUrl.For("/builder").Should().Be("/account/login?returnUrl=%2Fbuilder");

    [Fact] // AUTH-11
    public void A_query_survives_the_round_trip()
        => SignInUrl.For("/builder?empty=true").Should().Be("/account/login?returnUrl=%2Fbuilder%3Fempty%3Dtrue");

    [Theory] // AUTH-12
    [InlineData("https://elsewhere.example/steal")]
    [InlineData("//elsewhere.example")]
    [InlineData("\\\\elsewhere.example")]
    [InlineData("")]
    [InlineData(null)]
    public void Anything_that_is_not_local_becomes_the_home_page(string? candidate)
    {
        // A return address arrives from a request, and one that leaves this application is an open
        // redirect, which is why the sanitiser is the one the account route already uses.
        SignInUrl.For(candidate).Should().Be("/account/login?returnUrl=%2F");
    }
}
