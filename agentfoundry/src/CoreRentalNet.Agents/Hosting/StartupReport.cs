using CoreRentalNet.Agents.Features.EchoReverse;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion;
using CoreRentalNet.Agents.Shared.Model;

namespace CoreRentalNet.Agents.Hosting;

/// <summary>Prints, before anything else can fail, what a deployment must supply and what it is serving.</summary>
/// <remarks>
/// Every setting a deployment must supply is named with only whether it is present — except the agent names, which
/// are not secrets and are what a container that is never acknowledged most needs to show. The writer is a
/// parameter so the report can be asserted without capturing the console.
/// </remarks>
internal static class StartupReport
{
    /// <summary>Prints the report to the console.</summary>
    public static void Write(AgentHostSettings settings)
        => Write(Console.Out, settings, IsHosted());

    /// <summary>Prints the report to the given writer, so a test can read what a startup says.</summary>
    internal static void Write(TextWriter writer, AgentHostSettings settings, bool hosted)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(settings);

        writer.WriteLine("[startup] host built; listening.");
        writer.WriteLine(string.Join("\n",
            "[startup] hosted agent deployable",
            $"[startup]   version                             : {settings.Build.Version}",
            $"[startup]   git sha                             : {settings.Build.GitSha ?? "(unset)"}",
            $"[startup]   build id                            : {settings.Build.BuildId ?? "(unset)"}",
            $"[startup]   environment                         : {settings.Build.Environment}",
            $"[startup]   hosted                              : {(hosted ? "yes" : "no")}",
            $"[startup]   {AgentFoundryRegistration.ProjectEndpointKey,-34}: {Present(settings.ProjectEndpoint)}",
            $"[startup]   {AgentFoundryRegistration.ModelKey,-34}: {Present(settings.ModelDeployment)}",
            $"[startup]   {"ModelTransport",-34}: {settings.ModelTransport.NetworkTimeout} per call, {settings.ModelTransport.MaximumRetryAttempts} transport retries",
            $"[startup]   {WorkspaceSuggestionAgentIdentity.ConfigurationKey,-34}: {(string.IsNullOrWhiteSpace(settings.WorkspaceSuggestion.AgentName) ? "MISSING" : settings.WorkspaceSuggestion.AgentName)}",
            $"[startup]   {EchoAgentIdentity.AgentNameKey,-34}: {settings.Echo.AgentName} ({(settings.Echo.IsEnabled ? "enabled" : "disabled")})",
            $"[startup]   {AgentHostSettings.DefaultAgentNameKey,-34}: {(string.IsNullOrWhiteSpace(settings.DefaultAgentName) ? "MISSING" : settings.DefaultAgentName)}"));
    }

    private static bool IsHosted()
        => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FOUNDRY_HOSTING_ENVIRONMENT"));

    private static string Present(string? value)
        => string.IsNullOrWhiteSpace(value) ? "MISSING" : "set";
}
