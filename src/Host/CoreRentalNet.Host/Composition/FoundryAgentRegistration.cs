using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.Core;
using Azure.Identity;
using CoreRentalNet.Host.Agents;
using CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Responses;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// Wires the application to the hosted agent, when one is configured.
/// </summary>
/// <remarks>
/// <para>
/// The shape is Microsoft's hosted-agent client sample: one <c>AIAgent</c> built one of two ways, and
/// consuming code that cannot tell which. A local endpoint is reached over http with a placeholder api
/// key and no credential at all; a Foundry project is reached over https with a token from the
/// application's own identity.
/// </para>
/// <para>
/// The client is the OpenAI-compatible one the platform's Responses protocol is documented to accept,
/// rather than the Foundry project client. That is a measured choice: the project client is beta and the
/// Foundry provider is preview, and the provider's endpoint-taking <c>AsAIAgent</c> exists only in the
/// framework's unreleased source - the sample uses project references to it. The cost of the choice is
/// named where it is paid: the OpenAI client's Responses surface is marked evaluation-only.
/// </para>
/// <para>
/// A deployment with no endpoint configured gets an agent that reports itself unconfigured, so the
/// operation exists everywhere and answers "unavailable" rather than the application failing to start.
/// That is a state, not a refusal of the request.
/// </para>
/// </remarks>
internal static class FoundryAgentRegistration
{
    /// <summary>The model name the client requires. A hosted agent ignores it.</summary>
    private const string Model = "hosted-agent";

    public static void AddFoundryAgent(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = FoundryAgentSettings.From(builder.Configuration);

        // Always registered, so the operation's dependencies are complete wherever the application
        // runs: a deployment with no agent gets one that says so rather than a start-up failure.
        builder.Services.AddSingleton<IAgentSuggestions>(settings.IsConfigured
            ? new FoundryAgentAdapter(new ResponsesAgentTextStream(Agent(settings)), settings.AgentName)
            : new NoAgentConfigured());
    }

    /// <summary>The agent, built against whichever endpoint is configured.</summary>
    private static AIAgent Agent(FoundryAgentSettings settings)
        => settings.IsLocal() ? LocalAgent(settings) : HostedAgent(settings);

    /// <summary>
    /// A local endpoint: the standard Responses route, reached with a placeholder key.
    /// </summary>
    /// <remarks>
    /// The stand-in hosts its own agent and ignores the key, but the client requires one to shape the
    /// request - the same trade Microsoft's sample makes. This is the hermetic tier's path: no
    /// credential, no token and no TLS, which is why it can run in a test suite.
    /// </remarks>
    private static AIAgent LocalAgent(FoundryAgentSettings settings)
        => new OpenAIClient(new ApiKeyCredential("not-needed"), new OpenAIClientOptions
        {
            Endpoint = new Uri(settings.Endpoint!),
        })
            .GetResponsesClient()
            .AsAIAgent(model: Model, name: settings.AgentName ?? "LocalStandInAgent");

    /// <summary>
    /// A Foundry project: the per-agent endpoint the platform routes to the container's own route.
    /// </summary>
    /// <remarks>
    /// The token comes from the application's own identity, named rather than discovered: a developer
    /// credential on a workstation and a managed identity where the application runs, so a deployment
    /// that has neither fails with a message about the credential. No secret is stored either way.
    /// </remarks>
    private static AIAgent HostedAgent(FoundryAgentSettings settings)
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(settings.AgentEndpoint()),
        };

        options.AddPolicy(
            new ApplicationTokenPolicy(Credential(settings), FoundryAgentSettings.Scope),
            PipelinePosition.BeforeTransport);

        // The api key is a placeholder the policy replaces with a token: the client insists on one to
        // shape the request, and the header it would write is overwritten before the request leaves.
        return new OpenAIClient(new ApiKeyCredential("supplied-by-the-bearer-token-policy"), options)
            .GetResponsesClient()
            .AsAIAgent(model: Model, name: settings.AgentName ?? Model);
    }

    private static TokenCredential Credential(FoundryAgentSettings settings)
        => settings.UseManagedIdentity
            ? new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned)
            : new AzureCliCredential();
}
