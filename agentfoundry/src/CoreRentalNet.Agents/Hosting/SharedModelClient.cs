using CoreRentalNet.Agents.Shared.Model;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Agents.Hosting;

/// <summary>Builds the one model client every feature's stages share.</summary>
/// <remarks>
/// Kept out of the composition root so the fatal line is stated once, where it belongs, rather than beside the
/// registrations. It refuses startup by name when a setting is missing, so a container that is never ready says
/// which setting rather than only that it was not ready.
/// </remarks>
internal static class SharedModelClient
{
    /// <summary>The model client, or a startup failure that names the setting nobody set.</summary>
    public static IChatClient BuildSharedModelClient(
        this AgentHostBuilder builder,
        ModelTransportRetryOptions modelTransport)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(modelTransport);

        try
        {
            var credential = FoundryCredential.Create(
                builder.WebApplicationBuilder.Environment,
                builder.Configuration
            );

            return AgentFoundryRegistration.BuildChatClient(builder.Configuration, credential, modelTransport);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"[startup] FATAL, the model client could not be built: {exception.Message}");
            throw;
        }
    }
}
