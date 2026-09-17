namespace AgentFoundry.WorkspaceSuggestions.Intent;

/// <summary>One row of the intent table: the phrasings that mean a slot set, and the set.</summary>
/// <remarks>
/// Infrastructure-only, the shape the file is read into. The phrases are normalised on load, so the
/// file can be written the way a person writes and matched the way the code reads.
/// </remarks>
internal sealed class IntentRow
{
    public List<string>? Phrases { get; set; }

    public List<string>? Slots { get; set; }
}
