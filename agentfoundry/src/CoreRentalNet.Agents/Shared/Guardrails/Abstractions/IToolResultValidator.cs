namespace CoreRentalNet.Agents.Shared.Guardrails.Abstractions;

/// <summary>Knows the shape one tool's answer must have, without the shared guard knowing the tool.</summary>
/// <remarks>
/// The port exists so the schema guard stays generic: the catalogue's contract is the feature's, and a shared
/// guard that named it would drag a workspace type into Shared.
/// </remarks>
internal interface IToolResultValidator
{
    bool AppliesTo(string toolName);

    bool IsValid(object? result);
}
