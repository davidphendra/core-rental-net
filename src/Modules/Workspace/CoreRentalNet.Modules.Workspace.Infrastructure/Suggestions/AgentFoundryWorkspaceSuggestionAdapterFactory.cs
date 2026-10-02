using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>Builds the remote agent the adapter talks to, one of two ways.</summary>
/// <remarks>
/// <para>
/// The only class in the application that touches an Azure type, and the only place a credential is resolved.
/// <c>AzureCliCredential</c> is the developer's own <c>az login</c>: it answers on a machine that has signed
/// in, and it never probes managed identity or workload identity, which is the ~100-second stall a
/// <c>DefaultAzureCredential</c> pays on a laptop before it reaches the same sign-in. <b>A deployment must
/// replace it with a managed identity</b> - a container has no CLI to answer - and the agent's own host makes
/// the same choice the same way.
/// </para>
/// <para>
/// <b>There are two paths, and the difference is the scheme rather than a setting.</b> A Foundry project is
/// reached over https with a bearer token; a local stand-in is reached over http with a placeholder api key
/// and no credential at all. That is not a shortcut around the real client - the protocol, the streaming and
/// the parsing are the same - it is the one difference a stand-in cannot imitate.
/// </para>
/// </remarks>
internal static class AgentFoundryWorkspaceSuggestionAdapterFactory
{
    /// <summary>The key the local path shapes its requests with. A stand-in never reads it.</summary>
    private const string PlaceholderKey = "not-needed";

    /// <summary>How long one call to the agent may wait on the network before the client gives up on it.</summary>
    /// <remarks>
    /// <para>
    /// A run carries a large context and streams for the better part of a minute, so it can legitimately take
    /// longer than the <c>System.ClientModel</c> default of 100 seconds. The client does not surface a slow
    /// call as slow: it cancels it at the default and retries, so one slow call becomes four timeouts and the
    /// run fails with <c>Retry failed after 4 tries</c>. The budget is stated here rather than left to that
    /// default.
    /// </para>
    /// <para>
    /// <b>This is not a run deadline.</b> It is the network timeout of one call to the agent - the point at
    /// which the transport is treated as dead rather than slow. The run has no deadline of the application's
    /// own: it ends when the agent answers, when it fails, or when the customer stops it, and the customer's
    /// cancellation is what bounds a run that hangs. The agent's own model client states the same budget on
    /// its side of the wire.
    /// </para>
    /// </remarks>
    public static readonly TimeSpan AgentNetworkTimeout = TimeSpan.FromMinutes(10);

    /// <summary>The per-agent endpoint the hosted agent answers on, derived rather than configured.</summary>
    public static Uri AgentEndpoint(AgentFoundryConnectionSetting connectionSetting)
    {
        ArgumentNullException.ThrowIfNull(connectionSetting);

        return new Uri(
            $"{connectionSetting.ProjectEndpoint.TrimEnd('/')}/agents/{connectionSetting.AgentName}/endpoint/protocols/openai");
    }

    /// <summary>The agent, built against whichever endpoint is configured.</summary>
    public static AIAgent Build(AgentFoundryConnectionSetting connectionSetting)
    {
        ArgumentNullException.ThrowIfNull(connectionSetting);

        return connectionSetting.IsLocal() ? LocalChat(connectionSetting) : Hosted(connectionSetting);
    }

    /// <summary>
    /// The credential-free client a stand-in is reached with: the same Responses client the hosted path uses.
    /// </summary>
    internal static AIAgent LocalChat(AgentFoundryConnectionSetting connectionSetting)
    {
        ArgumentNullException.ThrowIfNull(connectionSetting);

#pragma warning disable OPENAI001 // The Responses surface: documented, evaluation-marked, and the only plain-http path.
        return new OpenAIClient(
                new ApiKeyCredential(PlaceholderKey),
                OptionsForTheLocalClient(connectionSetting))
            .GetResponsesClient()
            .AsAIAgent(model: connectionSetting.AgentName, name: "LocalHostedAgent");
#pragma warning restore OPENAI001
    }

    /// <summary>The options a stand-in is reached with, including the policy that states the run's token.</summary>
    private static OpenAIClientOptions OptionsForTheLocalClient(
        AgentFoundryConnectionSetting connectionSetting)
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(connectionSetting.ProjectEndpoint),
            NetworkTimeout = AgentNetworkTimeout,
        };

        SetupRunScopeAccessTokenPolicy(options);

        return options;
    }

    /// <summary>The options a Foundry project is reached with, including the same policy.</summary>
    private static AIProjectClientOptions OptionsForTheHostedClient(
        AgentFoundryConnectionSetting connectionSetting)
    {
        var options = new AIProjectClientOptions
        {
            NetworkTimeout = AgentNetworkTimeout
        };

        SetupRunScopeAccessTokenPolicy(options);

        return options;
    }

    /// <summary>States the run's own token on every request either client sends.</summary>
    /// <remarks>
    /// <b>On the pipeline rather than at a call site, because the agent is built once and shared.</b> The token
    /// belongs to the run, so it is read from the run's own scope as each request is built — and the two clients
    /// take different option types, so this is the base they share rather than a copy of the same three lines.
    /// <c>PerCall</c> rather than <c>PerTry</c>: a retry of the same call re-states the token it already had.
    /// </remarks>
    private static void SetupRunScopeAccessTokenPolicy(ClientPipelineOptions clientOptions)
        => clientOptions.AddPolicy(new AccessTokenPipelinePolicy(), PipelinePosition.PerCall);

    /// <summary>A Foundry project: the per-agent endpoint, reached with the application's own identity.</summary>
    /// <remarks>
    /// The options are not decoration. <c>AsAIAgent(client, endpoint)</c> reuses this project client's pipeline
    /// rather than building its own, so the network timeout stated here is the one the agent call runs under;
    /// without it the call inherits the 100-second default and a long run is cancelled and retried.
    /// </remarks>
    private static AIAgent Hosted(AgentFoundryConnectionSetting connectionSetting)
        => new AIProjectClient(
                new Uri(connectionSetting.ProjectEndpoint),
                new AzureCliCredential(),
                OptionsForTheHostedClient(connectionSetting))
            .AsAIAgent(AgentEndpoint(connectionSetting));
}
