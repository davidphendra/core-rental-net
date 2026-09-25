// The workspace-suggestion agent's host.
//
// Shape verified by scaffolding and building against the pinned packages: AgentHost.CreateBuilder from
// Azure.AI.AgentServer.Core binds the port, serves the readiness probe and wires OpenTelemetry;
// AddFoundryResponses registers the Responses server and the agent; RegisterProtocol maps it.
//
// There is no catalogue connection here. The tools are per caller — the MCP handshake is authenticated — so
// the served agent is registered per request and the pipeline is built inside the run with the tools that
// run's own token returned.

using Azure.AI.AgentServer.Core;
using DotNetEnv;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry.Hosting;
using Microsoft.Extensions.AI;
using WorkspaceSuggestions.Agents;
using WorkspaceSuggestions.Composition;
using WorkspaceSuggestions.Contracts;
using WorkspaceSuggestions.Helpers;
using WorkspaceSuggestions.Tools;
using WorkspaceSuggestions.Workflows;

// Local development only: a .env file is ignored if absent, and nothing is ever injected in a hosted
// container.
Env.NoClobber().TraversePath().Load();

var builder = AgentHost.CreateBuilder(args);

// One model client, shared by every agent: the roster's agents are built from it, keyed by name.
var chatClient = AgentFoundryRegistration.BuildChatClient(builder.Configuration);

builder.Services.AddSingleton(chatClient);

// The catalogue's endpoint, and the credential the call presents to it: the caller's own, held per request.
var catalogSettings = CatalogToolSettings.From(builder.Configuration);
builder.Services.AddSingleton(catalogSettings);
builder.Services.AddScoped<IMcpAccessTokenService, McpAccessTokenService>();
builder.Services.AddScoped<IMcpToolRequest<SuggestionRequest>, SuggestionRequestSplitter>();

// The pipeline, as a port: the composition names the interface, not the class.
builder.Services.AddScoped<IWorkspaceWorkflow, WorkspaceSuggestionWorkflow>();

// Registered per request, so the tools are the caller's and the agent that carries them does not outlive the
// call. The tool list is what that caller's token returned, and nothing is shared between callers.
builder.Services.AddRosterAgents(chatClient, []);
builder.Services.AddKeyedScoped<AIAgent>(WorkspaceSuggestionWorkflow.AgentName, (provider, _) =>
    new McpToolsAccessAgent<SuggestionRequest>(
        provider.GetRequiredService<IWorkspaceWorkflow>(),
        provider.GetRequiredService<CatalogToolSettings>(),
        provider.GetRequiredService<IMcpAccessTokenService>(),
        provider.GetRequiredService<IMcpToolRequest<SuggestionRequest>>()));

// Resolves the agent for a request from keyed DI by agent.name — no instance is handed over.
builder.Services.AddFoundryResponses();
builder.RegisterProtocol("responses", endpoints => endpoints.MapFoundryResponses());

var app = builder.Build();
app.Run();
