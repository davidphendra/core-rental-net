using AwesomeAssertions;
using CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// The two facts the application side of the carrier depends on: the header's name is one the platform forwards,
/// and the token is carried for exactly as long as the run that owns it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The prefix is a requirement rather than a style choice.</b> The hosting layer's request context exposes the
/// client headers — those prefixed with <c>x-client-</c> — so a name outside that prefix is dropped, and a
/// dropped token is indistinguishable from a catalogue refusal at the far end. The agent states the same name in
/// its own constant, because the two are separate deployables with no shared assembly; a test pins the prefix on
/// each side rather than trusting two copies to agree.
/// </para>
/// <para>
/// <b>Whether the platform actually forwards it is not testable here.</b> That is a property of the deployed
/// proxy, so this file pins what our own code is responsible for and leaves the rest to the deployed probe.
/// </para>
/// </remarks>
public sealed class AgentInvocationHeaderTests
{
    private const string TheCallersToken = "header.eyJzdWIiOiJjdXN0b21lci0xIn0.signature";

    [Fact]
    public void The_invocation_header_carries_the_platforms_client_prefix()
        => AgentFoundryInvocationHeaders.AccessToken.Should().StartWith(
            "x-client-",
            "the platform forwards client headers only under this prefix, and a dropped token is silent");

    [Fact]
    public void The_token_is_carried_for_the_run_that_started_the_scope()
    {
        using var callerAccessTokenForTheRun =
            RunScopeAccessToken.CarryTheTokenOf(TheCallersToken);

        RunScopeAccessToken.AccessToken.Should().Be(TheCallersToken);
    }

    [Fact] // the scope is a scope, not a flag: a nested run gives the enclosing run its token back
    public void An_inner_scope_gives_the_outer_run_its_token_back_when_it_ends()
    {
        using var outerRun = RunScopeAccessToken.CarryTheTokenOf(TheCallersToken);

        using (RunScopeAccessToken.CarryTheTokenOf("another-run-s-token"))
        {
            RunScopeAccessToken.AccessToken.Should().Be("another-run-s-token");
        }

        RunScopeAccessToken.AccessToken.Should().Be(
            TheCallersToken,
            "a finished run must not be able to erase the token of the run that encloses it");
    }

    [Fact] // and nothing is carried when no run is in progress, so no request can inherit one
    public void No_token_is_carried_when_no_run_is_in_progress()
        => RunScopeAccessToken.AccessToken.Should().BeNull();
}
