namespace CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

/// <summary>What the reviewer objected to, as the contract carries it.</summary>
/// <remarks>
/// A kind and a slot rather than a sentence: the agent's contract forbids prose, so the objection
/// arrives structured and the application decides how to say it. The slot is kept because dropping it
/// here would leave the application able to say only that something was wrong, never what.
/// </remarks>
public sealed record AgentFinding(string Kind, string Slot);
