namespace AgentFoundry.WorkspaceSuggestions.Contracts;

/// <summary>A criterion the catalogue cannot express, carried rather than dropped.</summary>
/// <remarks>
/// The phrase is the customer's own word, quoted. It is the only free text in this contract, and it
/// comes from the request rather than from the model: what the model wrote is never rendered as copy.
/// </remarks>
public sealed record UnevaluatedCriterion(string Phrase, string Reason);
