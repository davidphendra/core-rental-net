using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using WorkspaceSuggestions.Prompts;

namespace WorkspaceSuggestions.Agents;

/// <summary>Turns a profile into the agent the framework runs.</summary>
/// <remarks>
/// The only place an <see cref="AIAgent"/> is constructed from the roster, so every agent is built the same
/// way: its own prompt, loaded and versioned by <see cref="EmbeddedInstructionSource"/>, and its own output
/// contract taken from the profile. The chat client is a parameter rather than something built here, which is
/// what lets the tests run the same code against a fake — no network, no model, no credential.
/// </remarks>
internal static class AgentFactory
{
    public static AIAgent Build(
        AgentProfile profile,
        IChatClient client,
        IReadOnlyList<AITool>? tools = null)
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
                    // An agent with no catalogue to read is given no tools, not an empty list: the framework
                    // sends what is here, and an empty tool array would say the model may call nothing.
                    Tools = tools is { Count: > 0 } ? [.. tools] : null,
                },
            });
    }
}
