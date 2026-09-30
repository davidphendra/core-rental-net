using Azure.AI.AgentServer.Core;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.AI;
using System.ClientModel.Primitives;
using CoreRentalNet.Agents.Shared.Model;

namespace CoreRentalNet.Agents.Shared.Model;

/// <summary>Builds the one model client this host shares.</summary>
/// <remarks>
/// The only place a Foundry client is created. Everything downstream takes an <see cref="IChatClient"/> and an
/// <c>AIAgent</c>, which is what lets the whole agent tree be exercised offline against a fake.
/// </remarks>
internal static class AgentFoundryRegistration
{
    /// <summary>The deployment the run reports having used.</summary>
    public const string ModelKey = "AZURE_AI_MODEL_DEPLOYMENT_NAME";

    /// <summary>How long one model call may take before the client gives up on it.</summary>
    /// <remarks>
    /// One run carries a large context — the specification, the catalogue results and the answer schema — so a
    /// model call can legitimately take longer than the 100-second <c>System.ClientModel</c> default. The
    /// client does not surface a slow call as slow: it cancels it at the default and retries, so one slow call
    /// becomes four timeouts and the run fails with "Retry failed after 4 tries". The budget is stated here
    /// rather than left to that default.
    /// </remarks>
    public static readonly TimeSpan ModelNetworkTimeout = new ModelTransportRetryOptions().NetworkTimeout;

    /// <summary>The model client, built once. Configuration carries a project endpoint and a deployment name.</summary>
    /// <remarks>
    /// The transport's own retry budget is stated here and is <b>not</b> the workflow's attempt count: this
    /// bounds how many times one failed call is retried, and the workflow bounds how many times the sentence is
    /// read. Sharing one counter between them would make a retried call look like a retried attempt.
    /// </remarks>
    public static IChatClient BuildChatClient(
        IConfiguration configuration,
        ModelTransportRetryOptions? modelTransportRetryOptions = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var endpoint = Required(configuration, "FOUNDRY_PROJECT_ENDPOINT");
        var model = Required(configuration, ModelKey);
        var transportRetryOptions = modelTransportRetryOptions ?? new ModelTransportRetryOptions();

        var openAiOptions = new ProjectOpenAIClientOptions
        {
            NetworkTimeout = transportRetryOptions.NetworkTimeout,
            RetryPolicy = new ClientRetryPolicy(transportRetryOptions.MaximumRetryAttempts),
        };

        return new AIProjectClient(new Uri(endpoint), Credential())
            .GetProjectOpenAIClient(openAiOptions)
            .GetChatClient(model)
            .AsIChatClient();
    }

    /// <summary>The credential this process presents to Foundry, chosen for where the process is running.</summary>
    /// <remarks>
    /// <para>
    /// In a hosted container the whole chain is the point: managed identity answers in milliseconds, so the
    /// default is what should be used.
    /// </para>
    /// <para>
    /// On a developer's machine it is the opposite. Managed identity and workload identity have no source to
    /// answer from, and <c>DefaultAzureCredential</c> still probes them before it reaches the developer's own
    /// sign-in — a probe that takes about a hundred seconds to fail. The model client's network timeout is
    /// also a hundred seconds, so the first token request is cancelled while it is still probing, retried, and
    /// cancelled again: the run dies as "Retry failed after 4 tries" without ever reaching the model. Excluding
    /// those two sources turns the first token request into the developer's own sign-in, which answers at once.
    /// </para>
    /// </remarks>
    private static TokenCredential Credential()
        => FoundryEnvironment.IsHosted
            ? new DefaultAzureCredential()
            : new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeManagedIdentityCredential = true,
                ExcludeWorkloadIdentityCredential = true,
            });

    /// <summary>A setting a deployment must supply, or a failure that names it.</summary>
    /// <remarks>
    /// Whitespace counts as absent, not as a value. An environment variable set to nothing is the shape a
    /// half-finished deployment actually has, and passing it through would surface as a
    /// <c>UriFormatException</c> from the line that happens to use it rather than as the name of the setting
    /// nobody set.
    /// </remarks>
    private static string Required(IConfiguration configuration, string key)
        => configuration[key]?.Trim() is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"{key} is not configured.");
}
