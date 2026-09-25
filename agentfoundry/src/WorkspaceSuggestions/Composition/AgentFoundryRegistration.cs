using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using WorkspaceSuggestions.Agents;
using WorkspaceSuggestions.Prompts;
using WorkspaceSuggestions.Workflows;

namespace WorkspaceSuggestions.Composition;

/// <summary>Builds the agents and the pipeline this host serves.</summary>
/// <remarks>
/// The only place a Foundry client is created. Everything downstream takes an <see cref="IChatClient"/> and an
/// <see cref="AIAgent"/>, which is what lets the whole agent tree be exercised offline against a fake.
/// </remarks>
internal static class AgentFoundryRegistration
{
    /// <summary>The deployment the run reports having used.</summary>
    public const string ModelKey = "AZURE_AI_MODEL_DEPLOYMENT_NAME";

    /// <summary>The model client, built once. Configuration carries a project endpoint and a deployment name.</summary>
    /// <remarks>
    /// It comes back wrapped, here and once: the counting belongs to the client every agent shares, and it
    /// does nothing at all unless a run is in flight.
    /// </remarks>
    public static IChatClient BuildChatClient(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var endpoint = Required(configuration, "FOUNDRY_PROJECT_ENDPOINT");
        var model = Required(configuration, ModelKey);

        var client = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
            .GetProjectOpenAIClient()
            .GetChatClient(model)
            .AsIChatClient();

        return client;
    }

    /// <summary>The deployment name, so a deployment can name the model it runs.</summary>
    public static string ModelName(IConfiguration configuration)
        => Required(configuration, ModelKey);

    /// <summary>Registers every agent on the roster under its own name, which is also its executor identity.</summary>
    public static IServiceCollection AddRosterAgents(
        this IServiceCollection services,
        IChatClient client,
        IReadOnlyList<AITool> tools)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(tools);

        foreach (var profile in AgentRoster.All)
        {
            services.AddKeyedSingleton<AIAgent>(profile.Name, AgentFactory.Build(profile, client, ToolsFor(profile, tools)));
        }

        return services;
    }

    /// <summary>The workflow, published as the single agent the Foundry host serves.</summary>
    /// <remarks>
    /// The pipeline is the workflow's to build, and it is built per call with the tools that call may use; this
    /// is the convenience the composition root and the tests share. <paramref name="model"/> is carried for the
    /// deployments that name it and is not otherwise read here.
    /// </remarks>
    public static AIAgent BuildWorkflowAgent(IChatClient client, string model, IReadOnlyList<AITool> tools)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(tools);

        return new WorkspaceSuggestionWorkflow(client).AsAIAgent(tools);
    }

    /// <summary>The tools one agent is given: the suggestor reads the catalogue, and the rephraser does not.</summary>
    /// <remarks>
    /// The rephraser turns a sentence into a specification and names no product, so a tool would let it choose
    /// one before the specification exists. The name is also the identity the workflow records, which is what
    /// this keys on - the same name the roster is registered under.
    /// </remarks>
    internal static IReadOnlyList<AITool> ToolsFor(AgentProfile profile, IReadOnlyList<AITool> tools)
        => profile.Name == AgentRoster.Suggestor.Name ? tools : [];

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
