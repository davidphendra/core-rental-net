// The workspace-suggestion agent's host.
//
// Shape verified by scaffolding and building against the pinned packages: AgentHost.CreateBuilder from
// Azure.AI.AgentServer.Core binds the port, serves the readiness probe and wires OpenTelemetry;
// AddFoundryResponses registers the Responses server and the agent; RegisterProtocol maps it.

using Azure.AI.AgentServer.Core;
using DotNetEnv;
using Microsoft.Agents.AI.Foundry.Hosting;
using WorkspaceSuggestions.Composition;
using WorkspaceSuggestions.Tools;

// Local development only: a .env file is ignored if absent, and nothing is ever injected in a hosted
// container.
Env.NoClobber().TraversePath().Load();

var builder = AgentHost.CreateBuilder(args);

// One model client, shared by every agent: the roster's agents are built from it, keyed by name.
var chatClient = AgentRegistration.BuildChatClient(builder.Configuration);

// The catalogue's tools, discovered once from its MCP server. An unconfigured catalogue is no tools rather
// than a broken agent; a configured one that cannot be reached stops the host, because an agent with no
// tools would answer every run from memory. Disposed with the host, once app.Run returns.
var settings = CatalogToolSettings.From(builder.Configuration);
await using var mcpCatalogueClient = await McpCatalogTools.ConnectAsync(
    settings,
    new Auth0CatalogAccessTokenService(settings, new HttpClient()));

builder.Services.AddRosterAgents(chatClient, mcpCatalogueClient.Tools);

// The host serves the workflow, not an agent: rephraser then suggestor, published as one. Built eagerly
// rather than resolved from DI, because AddFoundryResponses takes the agent instance. Only the suggestor is
// handed the catalogue's tools.
var workflowSuggestorAgent = AgentRegistration.BuildWorkflowAgent(
    chatClient,
    AgentRegistration.ModelName(builder.Configuration),
    mcpCatalogueClient.Tools);

builder.Services.AddFoundryResponses(workflowSuggestorAgent);
builder.RegisterProtocol("responses", endpoints => endpoints.MapFoundryResponses());

var app = builder.Build();
app.Run();
