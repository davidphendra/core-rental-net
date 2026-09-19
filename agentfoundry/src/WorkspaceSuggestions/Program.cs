// The workspace-suggestion agent's host.
//
// Shape verified by scaffolding and building against the pinned packages: AgentHost.CreateBuilder from
// Azure.AI.AgentServer.Core binds the port, serves the readiness probe and wires OpenTelemetry;
// AddFoundryResponses registers the Responses server and the agent; RegisterProtocol maps it.

using Azure.AI.AgentServer.Core;
using DotNetEnv;
using Microsoft.Agents.AI.Foundry.Hosting;
using WorkspaceSuggestions.Composition;

// Local development only: a .env file is ignored if absent, and nothing is ever injected in a hosted
// container.
Env.NoClobber().TraversePath().Load();

var builder = AgentHost.CreateBuilder(args);

// One model client, shared by every agent: the roster's agents are built from it, keyed by name.
var client = AgentRegistration.BuildChatClient(builder.Configuration);
builder.Services.AddRosterAgents(client);

// The host serves the workflow, not an agent: rephraser then suggestor, published as one. Built eagerly
// rather than resolved from DI, because AddFoundryResponses takes the agent instance.
var agent = AgentRegistration.BuildWorkflowAgent(client, AgentRegistration.ModelName(builder.Configuration));

builder.Services.AddFoundryResponses(agent);
builder.RegisterProtocol("responses", endpoints => endpoints.MapFoundryResponses());

var app = builder.Build();
app.Run();
