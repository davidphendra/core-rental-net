using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace CoreRentalNet.Host.Agents;

/// <summary>Builds the remote agent the adapter talks to, one of two ways.</summary>
/// <remarks>
/// <para>
/// The only class in the application that touches an Azure type, and the only place a credential is resolved.
/// <c>DefaultAzureCredential</c> is deliberate: it resolves to the developer's sign-in locally and to a managed
/// identity when deployed, so no key ever appears in configuration.
/// </para>
/// <para>
/// The endpoint is <b>derived</b> — <c>&lt;project&gt;/agents/&lt;name&gt;/endpoint/protocols/openai</c> — so
/// configuration carries a project endpoint and an agent name, never a URL.
/// </para>
/// <para>
/// <b>There are two paths, and the difference is the scheme rather than a setting.</b> A Foundry project is
/// reached over https with a bearer token; a local stand-in is reached over http with a placeholder api key and
/// no credential at all. That is not a shortcut around the real client — the protocol, the streaming and the
/// parsing are the same — it is the one difference a stand-in cannot imitate, because there is nothing for it to
/// authenticate against. It is also not a choice: measured, the project client refuses to send a bearer to a
/// plain-http endpoint, so a stand-in cannot be reached the other way even if a credential could be found.
/// </para>
/// </remarks>
internal static class FoundryAgentFactory
{
    /// <summary>The key the local path shapes its requests with. A stand-in never reads it.</summary>
    private const string PlaceholderKey = "not-needed";

    /// <summary>The per-agent endpoint the hosted agent answers on, derived rather than configured.</summary>
    public static Uri AgentEndpoint(AgentFoundrySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new Uri(
            $"{settings.ProjectEndpoint.TrimEnd('/')}/agents/{settings.AgentName}/endpoint/protocols/openai");
    }

    /// <summary>The agent, built against whichever endpoint is configured.</summary>
    public static AIAgent Build(AgentFoundrySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings.IsLocal() ? StandIn(settings) : Hosted(settings);
    }

    /// <summary>
    /// The credential-free client a stand-in is reached with: the same Responses client the hosted path uses.
    /// </summary>
    /// <remarks>
    /// Exposed rather than inlined so that a test can hold <b>this</b> client's request shape against the wire.
    /// The structured-output format is a property of the client and the protocol - not of this application,
    /// which never sets a schema - so a test that rebuilt its own client would be testing a replica.
    /// </remarks>
    internal static IChatClient LocalChat(AgentFoundrySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

#pragma warning disable OPENAI001 // The Responses surface: documented, evaluation-marked, and the only plain-http path.
        return new OpenAIClient(
                new ApiKeyCredential(PlaceholderKey),
                new OpenAIClientOptions { Endpoint = new Uri(settings.ProjectEndpoint) })
            .GetResponsesClient()
            .AsIChatClient();
#pragma warning restore OPENAI001
    }

    /// <summary>
    /// A stand-in on this machine: the same Responses protocol, with no credential and no TLS.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the hermetic tier's path, and the reason the browser suite can exist at all: the application
    /// reads a real socket with the real client and the real streaming code, and nothing about the run is
    /// simulated. The client insists on a key to shape the request, and the stand-in ignores the header.
    /// </para>
    /// <para>
    /// <b>The evaluation-only marker is suppressed in one method and nowhere else.</b> The OpenAI client's
    /// Responses surface is the one the protocol is documented to accept for a client that is not a Foundry
    /// project, and it carries <c>OPENAI001</c>; the alternative is the project client, which refuses plain
    /// http outright. The suppression is one expression wide, so a second use of the surface has to be a
    /// deliberate second decision rather than something that inherits this one.
    /// </para>
    /// </remarks>
    private static AIAgent StandIn(AgentFoundrySettings settings)
        => new ChatClientAgent(LocalChat(settings), new ChatClientAgentOptions { Name = settings.AgentName });

    /// <summary>A Foundry project: the per-agent endpoint, reached with the application's own identity.</summary>
    private static AIAgent Hosted(AgentFoundrySettings settings)
        => new AIProjectClient(new Uri(settings.ProjectEndpoint), new DefaultAzureCredential())
            .AsAIAgent(AgentEndpoint(settings));
}
