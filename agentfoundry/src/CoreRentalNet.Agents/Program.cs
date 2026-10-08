// The hosted agent deployable's composition root: a list of host steps and features, and no logic of its own.
//
// Shape verified by scaffolding and building against the pinned packages: AgentHost.CreateBuilder from
// Azure.AI.AgentServer.Core binds the port, serves the readiness probe and wires OpenTelemetry;
// AddFoundryResponses registers the Responses server; RegisterProtocol maps it.
//
// Each feature registers what it needs — its graph, its stages, the MCP server it searches, its bounds — and each
// host step registers one concern. The served agent is always the feature's workflow agent itself: hosting can
// only redirect a hosted workflow's checkpoints when the agent it resolves is that workflow agent, so nothing
// wraps it.

using CoreRentalNet.Agents.Features.EchoReverse;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion;
using CoreRentalNet.Agents.Hosting;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Shared.Scoping;
using Microsoft.Agents.AI.Foundry.Hosting;

// Local development only; a hosted container ships no .env file, so this is a no-op there.
LocalEnvironmentDefaults.Apply();

var builder = AgentHost.CreateBuilder(args);

// Everything the host reads from configuration, resolved once so that no two readers can disagree.
var settings = AgentHostSettings.FromConfiguration(builder.Configuration);

// The application's own source, meter, redaction and build identity, on the platform's providers.
builder.AddHostTelemetry(settings.Build);

// One model client, shared by every feature's stages.
var modelClient = builder.BuildSharedModelClient(settings.ModelTransport);
builder.Services.AddSingleton(modelClient);

// Shared plumbing: one token per call, read from the invocation's own header, and the run scope the served graph
// reaches it through.
builder.Services.AddScoped<IMcpAccessTokenService, McpAccessTokenService>();
builder.Services.AddSingleton<IRunScope, RunScope>();

// The features this deployable serves. A new feature is a new line here and a folder under Features/.
builder.Services.AddWorkspaceSuggestionFeature(
    builder.Configuration, settings.WorkspaceSuggestion, modelClient);
builder.Services.AddEchoReverseFeature(settings.Echo);

// A request that carries no name gets the agent this deployment is named for.
builder.Services.AddDefaultAgent(settings.DefaultAgentName);

builder.Services.AddHttpContextAccessor();
builder.Services.AddFoundryResponses();
builder.RegisterProtocol("responses", endpoints => endpoints.MapFoundryResponses());

var app = builder.Build();

StartupReport.Write(settings);

app.Run();
