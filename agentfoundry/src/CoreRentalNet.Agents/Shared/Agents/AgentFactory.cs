using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using CoreRentalNet.Agents.Shared.ChatClients;
using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;
using CoreRentalNet.Agents.Shared.Prompts;
using CoreRentalNet.Agents.Shared.Telemetry;

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
    /// <summary>
    /// Builds a stage agent. The served agent is never built here - it is the workflow agent, and hosting can
    /// only checkpoint an agent that is not wrapped.
    /// </summary>
    /// <remarks>
    /// The guardrail middleware is optional because a stage with no tools has nothing for it to guard; the echo
    /// stage therefore stays uncoupled from the guardrail layer rather than carrying a policy it never reaches.
    /// </remarks>
    public static AIAgent Build(
        AgentProfile profile,
        IChatClient client,
        bool captureContent = false,
        IGuardrailFunctionMiddleware? guardrailMiddleware = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(client);

        var instructions = new EmbeddedInstructionSource(profile.PromptFileName);

        var builder = new ChatClientAgent(
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
            })
            .AsBuilder()
            .UseOpenTelemetry(
                WorkspaceTelemetry.Name,
                configure: stageAgent => stageAgent.EnableSensitiveData = captureContent);

        if (guardrailMiddleware is not null)
        {
            builder = builder.Use(guardrailMiddleware.InvokeAsync);
        }

        return builder.Build();
    }
}
