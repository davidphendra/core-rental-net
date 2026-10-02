using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using CoreRentalNet.Agents.Shared.ChatClients;
using CoreRentalNet.Agents.Shared.Prompts;

namespace CoreRentalNet.Agents.Shared.Agents;

/// <summary>Turns a profile into the agent the framework runs.</summary>
/// <remarks>
/// The only place an <see cref="AIAgent"/> is constructed from the roster, so every agent is built the same
/// way: its own prompt, loaded and versioned by <see cref="EmbeddedInstructionSource"/>, and its own output
/// contract taken from the profile. The chat client is a parameter rather than something built here, which is
/// what lets the tests run the same code against a fake — no network, no model, no credential. The catalogue
/// tools are not set here either: they are the call's, and
/// <see cref="CoreRentalNet.Agents.Shared.ChatClients.AuthorisedMcpChatClient"/> offers them at the model call.
/// </remarks>
internal static class AgentFactory
{
    public static AIAgent Build(AgentProfile profile, IChatClient client)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(client);

        var instructions = new EmbeddedInstructionSource(profile.PromptFileName);

        return new ChatClientAgent(
            client,
            new ChatClientAgentOptions
            {
                Name = profile.Name,
                Description = profile.Description,
                ChatOptions = new ChatOptions
                {
                    Instructions = instructions.Text,
                    ResponseFormat = profile.Output,

                    // The stage names itself here so the run-scoped telemetry can attribute each model call to
                    // it. The framework merges these properties into the options of every call the agent makes.
                    AdditionalProperties = new AdditionalPropertiesDictionary
                    {
                        [ITelemetryChatClient.AgentName] = profile.Name,
                    },
                },
            });
    }
}
