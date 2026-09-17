namespace AgentFoundry.WorkspaceSuggestions.Contracts;

/// <summary>What the reviewer objected to, structured so the application renders it rather than quotes it.</summary>
public sealed record Finding(string Kind, string Slot);
