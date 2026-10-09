using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Text;
using AwesomeAssertions;
using CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;
using Microsoft.Agents.AI.Foundry;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>The hosted path's own wiring: the run's token has to be on the pipeline the endpoint client builds.</summary>
/// <remarks>
/// <para>
/// <b>The failure this pins was silent.</b> The agent-endpoint client builds a pipeline of its own from the
/// per-agent options, so a policy added to <c>AIProjectClientOptions</c> never reaches the request — the token
/// went out unset, which is indistinguishable from a catalogue that refused the caller. The local path uses a
/// different client whose pipeline <i>does</i> take the policy, which is why the defect only showed in Foundry.
/// </para>
/// <para>
/// The request is driven to the transport rather than asserted against SDK internals, because the transport is
/// the last place this application can state an observation: whatever the pipeline did is on the request that
/// arrives there, and nothing below it is the application's to know.
/// </para>
/// </remarks>
public sealed class AgentFoundryHostedWiringTests
{
    private const string TheCallersToken = "header.eyJzdWIiOiJjdXN0b21lci0xIn0.signature";

    [Fact] // the policy must be on the options the per-agent pipeline is built from, or the token never leaves
    public async Task The_hosted_agent_sends_the_runs_token_on_the_client_header()
    {
        var handler = new TheAgentsEndpoint();
        var options = AgentFoundryWorkspaceSuggestionAdapterFactory.OptionsForTheHostedAgent();
        options.Transport = new HttpClientPipelineTransport(new HttpClient(handler));

        var agent = new FoundryAgent(
            new Uri("https://example.invalid/api/projects/p/agents/an-agent/endpoint/protocols/openai"),
            new StubTokenProvider(),
            options);

        var previous = RunScopeAccessToken.Current;

        try
        {
            RunScopeAccessToken.Current = new RunContext(TheCallersToken);

            await agent.RunAsync("a sentence");
        }
        finally
        {
            RunScopeAccessToken.Current = previous;
        }

        handler.CarriedToken.Should().Be(
            TheCallersToken,
            "the agent-endpoint client builds its own pipeline, so the policy has to be on the options it is built from");
    }

    /// <summary>The agent endpoint as a transport sees it: it records the header and answers the run.</summary>
    private sealed class TheAgentsEndpoint : HttpMessageHandler
    {
        /// <summary>What a completed Responses call answers with, so the run has something to read.</summary>
        private const string CompletedRun = """
            {
              "id": "resp_1",
              "object": "response",
              "created_at": 0,
              "status": "completed",
              "model": "gpt-4.1-mini",
              "output": [
                {
                  "id": "msg_1",
                  "type": "message",
                  "status": "completed",
                  "role": "assistant",
                  "content": [ { "type": "output_text", "text": "ok", "annotations": [] } ]
                }
              ],
              "usage": { "input_tokens": 1, "output_tokens": 1, "total_tokens": 2 }
            }
            """;

        public string? CarriedToken { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CarriedToken = request.Headers.TryGetValues(
                AgentFoundryInvocationHeaders.AccessToken, out var carried)
                ? string.Concat(carried)
                : null;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CompletedRun, Encoding.UTF8, "application/json"),
            });
        }
    }

    /// <summary>The credential a test presents, so no identity provider is asked anything.</summary>
    private sealed class StubTokenProvider : AuthenticationTokenProvider
    {
        public override GetTokenOptions? CreateTokenOptions(
            IReadOnlyDictionary<string, object> properties)
            => new(properties);

        public override AuthenticationToken GetToken(
            GetTokenOptions options,
            CancellationToken cancellationToken)
            => new("stub", "Bearer", DateTimeOffset.MaxValue);

        public override ValueTask<AuthenticationToken> GetTokenAsync(
            GetTokenOptions options,
            CancellationToken cancellationToken)
            => new(new AuthenticationToken("stub", "Bearer", DateTimeOffset.MaxValue));
    }
}
