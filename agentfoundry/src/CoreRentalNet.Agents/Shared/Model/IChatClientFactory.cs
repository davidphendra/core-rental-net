using Microsoft.Extensions.AI;

namespace CoreRentalNet.Agents.Shared.Model;

/// <summary>Builds the one chat client a host's features share.</summary>
/// <remarks>
/// The host selects a factory once; no feature or stage knows whether the client talks to Foundry or to a
/// local OpenAI-compatible server.
/// </remarks>
internal interface IChatClientFactory
{
    IChatClient Create();
}
