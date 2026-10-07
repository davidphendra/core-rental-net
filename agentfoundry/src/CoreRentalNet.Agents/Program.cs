// The hosted agent deployable's composition root.
//
// Shape verified by scaffolding and building against the pinned packages: AgentHost.CreateBuilder from
// Azure.AI.AgentServer.Core binds the port, serves the readiness probe and wires OpenTelemetry;
// AddFoundryResponses registers the Responses server; RegisterProtocol maps it.
//
// This file names features, not their parts. Each feature registers what it needs — its graph, its stages, the
// MCP server it searches, its bounds — and this file names the shared plumbing once. The served agent is always
// the feature's workflow agent itself: hosting can only redirect a hosted workflow's checkpoints when the agent
// it resolves is that workflow agent, so nothing is allowed to wrap it.

using Azure.AI.AgentServer.Core;
using DotNetEnv;
using Microsoft.Agents.AI.Foundry.Hosting;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using CoreRentalNet.Agents.Features.EchoReply;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion;
using CoreRentalNet.Agents.Shared.Diagnostics;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Shared.Model;
using CoreRentalNet.Agents.Shared.Telemetry;

// Local development only: a .env file is ignored if absent, and nothing is ever injected in a hosted
// container. A platform that maps a setting from an unset azd variable injects it empty, and NoClobber keeps
// the empty value, so an empty value is refilled from the same file — a local default stands in for a
// variable nobody set, while anything the platform actually set is left alone.
var localDefaults = Env.NoClobber().TraversePath().Load();

foreach (var (key, value) in localDefaults)
{
    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
    {
        Environment.SetEnvironmentVariable(key, value);
    }
}

// The build identity, read from the artifact's own metadata. Nothing here comes from the process environment:
// the release, the commit, the pipeline run and the environment are stamped into the assembly by the
// pipeline, so a container reports the build it was made from and nothing a process can set.
var build = AgentBuildIdentity.Current;

var builder = AgentHost.CreateBuilder(args);

// The application's own source, meter and redaction processor. The hosting platform builds the providers and
// chooses the exporters; this only tells them what our code emits and what must be scrubbed before it leaves.
// The source and the meter are added here because the distro cannot know about an application's own vocabulary.
// The build identity is added as a resource attribute, so every span and every metric carries it without a
// single instrument naming it; the keys are ours because the platform already fills service.*.
builder.ConfigureTracing(tracing => tracing
    .AddSource(WorkspaceTelemetry.Name)
    .AddProcessor(new TokenLeakRedactionProcessor())
    .ConfigureResource(resource => resource.AddAttributes(build.ResourceAttributes())));

builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics
    .AddMeter(WorkspaceTelemetry.Name)
    .ConfigureResource(resource => resource.AddAttributes(build.ResourceAttributes())));

// The hosting package sets the logger provider's resource after ours, so the identity cannot ride that
// resource. It is stamped on each log record instead, through a processor the hosting adds from these options.
builder.Services.Configure<OpenTelemetryLoggerOptions>(logging =>
    logging.AddProcessor(new AgentBuildIdentityLogProcessor(build)));

// The names hosting resolves requests by. Read from configuration — the platform's environment variable when it
// set one, appsettings.json otherwise — so the keyed registrations hosting looks agents up under are the names
// the deployment uses rather than constants a rename would silently disagree with. Neither is a secret, and a
// container that is never acknowledged most needs to show them.
var workspaceSuggestionAgentName = builder.Configuration[WorkspaceSuggestionAgentIdentity.ConfigurationKey];

// Which agent a request that names none should get.
//
// Two azd behaviours meet here, and neither one carries the answer. `azd ai agent run <service>` does NOT pass
// the service name through as FOUNDRY_AGENT_NAME locally - only the hosting platform injects that - and
// `azd ai agent invoke --local` sends a request with no agent name at all. So without being told, a local
// container answers every invoke with whichever agent its appsettings default names, which is why addressing
// `echo-agent` was answered by the workspace agent.
//
// A deployment that serves more than one agent therefore says which one it is, per service, in azure.yaml.
// Remotely the injected FOUNDRY_AGENT_NAME already answers it, so this is unset there and the two agree when
// it is set.
var defaultAgentName = builder.Configuration["AgentHost:DefaultAgentName"]?.Trim() is { Length: > 0 } namedAgent
    ? namedAgent
    : workspaceSuggestionAgentName;
var echoAgentName = builder.Configuration[EchoAgentIdentity.AgentNameKey];
var echoAgentEnabled = !bool.TryParse(builder.Configuration[EchoAgentIdentity.IsEnabledKey], out var echoIsEnabled)
    || echoIsEnabled;

// The model transport's own budget: how long one call may take, and how many times one failed call is retried.
// It is deliberately not a workflow's attempt count, which is configuration too and belongs to its feature.
var modelTransportRetryOptions =
    builder.Configuration.GetSection("ModelTransport").Get<ModelTransportRetryOptions>()
        ?? new ModelTransportRetryOptions();

// Startup diagnostics, printed before anything else can fail. Every setting a deployment must supply is named
// here with only whether it is present — except the agent names, which are not secrets.
Console.WriteLine(string.Join("\n",
    "[startup] hosted agent deployable",
    $"[startup]   version                             : {build.Version}",
    $"[startup]   git sha                             : {build.GitSha ?? "(unset)"}",
    $"[startup]   build id                            : {build.BuildId ?? "(unset)"}",
    $"[startup]   environment                         : {build.Environment}",
    $"[startup]   hosted                              : {(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FOUNDRY_HOSTING_ENVIRONMENT")) ? "no" : "yes")}",
    $"[startup]   FOUNDRY_PROJECT_ENDPOINT            : {Present(builder.Configuration["FOUNDRY_PROJECT_ENDPOINT"])}",
    $"[startup]   {AgentFoundryRegistration.ModelKey,-34}: {Present(builder.Configuration[AgentFoundryRegistration.ModelKey])}",
    $"[startup]   {"ModelTransport",-34}: {modelTransportRetryOptions.NetworkTimeout} per call, {modelTransportRetryOptions.MaximumRetryAttempts} transport retries",
    $"[startup]   {WorkspaceSuggestionAgentIdentity.ConfigurationKey,-34}: {(string.IsNullOrWhiteSpace(workspaceSuggestionAgentName) ? "MISSING" : workspaceSuggestionAgentName)}",
    $"[startup]   {EchoAgentIdentity.AgentNameKey,-34}: {(string.IsNullOrWhiteSpace(echoAgentName) ? EchoAgentIdentity.DefaultAgentName : echoAgentName)} ({(echoAgentEnabled ? "enabled" : "disabled")})",
    $"[startup]   {"AgentHost:DefaultAgentName",-34}: {(string.IsNullOrWhiteSpace(defaultAgentName) ? "MISSING" : defaultAgentName)}"));

// One model client, shared by every feature's stages. Refuses startup by name when a setting is missing, so a
// container that is never ready says which one rather than only that it was not ready.
IChatClient modelClient;
try
{
    var credential = FoundryCredential.Create(builder.WebApplicationBuilder.Environment);
    modelClient = AgentFoundryRegistration.BuildChatClient(
        builder.Configuration, credential,
        modelTransportRetryOptions);
}
catch (Exception exception)
{
    Console.Error.WriteLine($"[startup] FATAL, the model client could not be built: {exception.Message}");
    throw;
}

Console.WriteLine("[startup] model client built; registering the features");

builder.Services.AddSingleton(modelClient);

// Shared plumbing: one token per call, read from the invocation's own header before a model is involved, and
// presented to whichever MCP server a feature searches.
builder.Services.AddScoped<IMcpAccessTokenService, McpAccessTokenService>();

// The features this deployable serves. A new feature is a new line here and a folder under Features/.
var servedWorkspaceSuggestionAgent = builder.Services.AddWorkspaceSuggestionFeature(
    builder.Configuration, modelClient);
var servedEchoAgent = builder.Services.AddEchoReplyFeature(builder.Configuration);

Console.WriteLine(string.Join("\n",
    $"[startup] serving {servedWorkspaceSuggestionAgent}",
    $"[startup] serving {servedEchoAgent ?? "(echo agent disabled)"}"));

// A request that names no agent gets the one this deployment is named for. Hosting falls back to a default
// agent when the request carries no name — which is exactly what `azd ai agent invoke --local` sends — so
// without this the fallback has nothing to fall back to and the request fails with
// "No agent name specified in the request (via agent.name or metadata["entity_id"]) and no default AIAgent is
// registered." The deployment's own name is the right default: a container serving `echo-agent` answers a
// nameless request with the echo, and one serving the workspace agent answers it with the workspace agent.
builder.Services.AddScoped<AIAgent>(serviceProvider =>
    serviceProvider.GetRequiredKeyedService<AIAgent>(defaultAgentName));

// Resolves an agent for a request from keyed DI by the name the request carries — no instance is handed over.
// The invocation's headers are read once, at the start of a run, by the reader the input stage holds.
builder.Services.AddHttpContextAccessor();
builder.Services.AddFoundryResponses();
builder.RegisterProtocol("responses", endpoints => endpoints.MapFoundryResponses());

var app = builder.Build();
Console.WriteLine("[startup] host built; listening. Agents resolve per request from keyed DI by agent.name.");
app.Run();

/// <summary>Whether a required setting is present, without ever printing it.</summary>
static string Present(string? value)
    => string.IsNullOrWhiteSpace(value) ? "MISSING" : "set";
