namespace AgentFoundry.WorkspaceSuggestions.Vocabularies;

/// <summary>
/// The seven slots a workspace can have, named once.
/// </summary>
/// <remarks>
/// The one place in this tree that lists them, asserted against the request schema rather than assumed
/// to match it. A slot arrives as a string, because the application owns the list and sends it with
/// every request; this is what validates that what arrived is one of them, and what a model's fallback
/// answer is checked against.
/// </remarks>
public static class Slots
{
    public const string Desk = "Desk";
    public const string Chair = "Chair";
    public const string Monitor = "Monitor";
    public const string Lamp = "Lamp";
    public const string Plant = "Plant";
    public const string CoffeeStation = "CoffeeStation";
    public const string RelaxZone = "RelaxZone";

    /// <summary>Every slot, in the order the catalogue publishes them.</summary>
    public static readonly IReadOnlyList<string> All =
        [Desk, Chair, Monitor, Lamp, Plant, CoffeeStation, RelaxZone];

    /// <summary>True when the text names one of them, ignoring letter case.</summary>
    public static bool IsKnown(string? slot)
        => slot is not null && All.Contains(slot, StringComparer.OrdinalIgnoreCase);
}
