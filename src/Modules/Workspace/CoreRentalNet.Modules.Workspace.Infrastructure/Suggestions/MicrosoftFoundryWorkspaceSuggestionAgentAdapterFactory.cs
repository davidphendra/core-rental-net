using System.ClientModel;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>Builds the remote agent the adapter talks to, one of two ways.</summary>
/// <remarks>
/// <para>
/// The only class in the application that touches an Azure type, and the only place a credential is resolved.
/// <c>DefaultAzureCredential</c> is deliberate: it resolves to the developer's sign-in locally and to a
/// managed identity when deployed, so no key ever appears in configuration.
/// </para>
/// <para>
/// <b>There are two paths, and the difference is the scheme rather than a setting.</b> A Foundry project is
/// reached over https with a bearer token; a local stand-in is reached over http with a placeholder api key
/// and no credential at all. That is not a shortcut around the real client - the protocol, the streaming and
/// the parsing are the same - it is the one difference a stand-in cannot imitate.
/// </para>
/// </remarks>
internal static class MicrosoftFoundryWorkspaceSuggestionAgentAdapterFactory
{
    /// <summary>The key the local path shapes its requests with. A stand-in never reads it.</summary>
    private const string PlaceholderKey = "not-needed";

    /// <summary>The per-agent endpoint the hosted agent answers on, derived rather than configured.</summary>
    public static Uri AgentEndpoint(MicrosoftFoundryAgentConnectionSettings connectionSettings)
    {
        ArgumentNullException.ThrowIfNull(connectionSettings);

        return new Uri(
            $"{connectionSettings.ProjectEndpoint.TrimEnd('/')}/agents/{connectionSettings.AgentName}/endpoint/protocols/openai");
    }

    /// <summary>The agent, built against whichever endpoint is configured.</summary>
    public static AIAgent Build(MicrosoftFoundryAgentConnectionSettings connectionSettings)
    {
        ArgumentNullException.ThrowIfNull(connectionSettings);

        return connectionSettings.IsLocal() ? StandIn(connectionSettings) : Hosted(connectionSettings);
    }

    /// <summary>
    /// The credential-free client a stand-in is reached with: the same Responses client the hosted path uses.
    /// </summary>
    internal static IChatClient LocalChat(MicrosoftFoundryAgentConnectionSettings connectionSettings)
    {
        ArgumentNullException.ThrowIfNull(connectionSettings);

#pragma warning disable OPENAI001 // The Responses surface: documented, evaluation-marked, and the only plain-http path.
        return new OpenAIClient(
                new ApiKeyCredential(PlaceholderKey),
                new OpenAIClientOptions { Endpoint = new Uri(connectionSettings.ProjectEndpoint) })
            .GetResponsesClient()
            .AsIChatClient();
#pragma warning restore OPENAI001
    }

    /// <summary>A stand-in on this machine: the same Responses protocol, with no credential and no TLS.</summary>
    private static AIAgent StandIn(MicrosoftFoundryAgentConnectionSettings connectionSettings)
        => new ChatClientAgent(LocalChat(connectionSettings), new ChatClientAgentOptions { Name = connectionSettings.AgentName });

    /// <summary>A Foundry project: the per-agent endpoint, reached with the application's own identity.</summary>
    private static AIAgent Hosted(MicrosoftFoundryAgentConnectionSettings connectionSettings)
        => new AIProjectClient(new Uri(connectionSettings.ProjectEndpoint), new DefaultAzureCredential())
            .AsAIAgent(AgentEndpoint(connectionSettings));
}
