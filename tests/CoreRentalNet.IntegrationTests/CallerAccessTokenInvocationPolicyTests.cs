using System.ClientModel.Primitives;
using AwesomeAssertions;
using CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>The application's own side of the carrier: the run's token becomes an invocation header.</summary>
/// <remarks>
/// <para>
/// The agent is built once and shared, so the token cannot be a property of it. It is read from the run's scope as
/// each request is built — which is what these facts pin, and which is why a request sent outside a run cannot
/// inherit a token from the run that came before it.
/// </para>
/// <para>
/// <b>The policy is driven where it runs rather than through a whole client send.</b> The fact under test is its
/// own <c>Process</c>; routing the message through the retry policy and a real transport would test those instead,
/// and would need an answer neither of them has here.
/// </para>
/// </remarks>
public sealed class CallerAccessTokenInvocationPolicyTests
{
    private const string TheCallersToken = "header.eyJzdWIiOiJjdXN0b21lci0xIn0.signature";

    [Fact]
    public void A_request_sent_during_a_run_carries_the_runs_token()
    {
        var policy = new CallerAccessTokenInvocationPolicy();
        using var message = MessageToTheAgent();

        using (CallerAccessTokenForTheRunScope.CarryTheTokenOf(TheCallersToken))
        {
            policy.Process(message, ChainEndingAtTheTransport(policy), 0);
        }

        HeaderOf(message).Should().Be(TheCallersToken);
    }

    [Fact] // and nothing is carried when no run is in progress, so no request can inherit one
    public void A_request_sent_outside_a_run_carries_nothing()
    {
        var policy = new CallerAccessTokenInvocationPolicy();
        using var message = MessageToTheAgent();

        policy.Process(message, ChainEndingAtTheTransport(policy), 0);

        HeaderOf(message).Should().BeNull("no run is in progress, so no request may inherit a token");
    }

    [Fact] // the agent reads this exact name on its own side of the wire, and the prefix is the one forwarded
    public void The_header_the_policy_states_is_one_the_platform_forwards()
        => MicrosoftFoundryAgentInvocationHeaders.CallerAccessToken.Should().StartWith(
            "x-client-",
            "a header outside this prefix is dropped, and a dropped token is silent");

    /// <summary>The policy under test followed by the end of the chain, which does not pass the message on.</summary>
    private static IReadOnlyList<PipelinePolicy> ChainEndingAtTheTransport(
        CallerAccessTokenInvocationPolicy policy)
        => [policy, new TheTransportThatNeverRuns()];

    private static PipelineMessage MessageToTheAgent()
    {
        var message = ClientPipeline.Create(new ClientPipelineOptions()).CreateMessage();

        message.Request.Method = "POST";
        message.Request.Uri = new Uri("https://foundry.example/agents/an-agent/endpoint/protocols/openai");

        return message;
    }

    private static string? HeaderOf(PipelineMessage message)
    {
        message.Request.Headers.TryGetValue(
            MicrosoftFoundryAgentInvocationHeaders.CallerAccessToken, out var carriedValue);

        return carriedValue;
    }
}
