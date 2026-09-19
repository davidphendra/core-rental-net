using System.Text.Json.Serialization;

namespace WorkspaceSuggestions.Contracts;

/// <summary>The slots a composition may fill. The names are the wire vocabulary and are PascalCase on purpose — they are the application's slot ids, not a catalogue category.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<WorkspaceSlot>))]
public enum WorkspaceSlot
{
    Desk,
    Chair,
    Monitor,
    Lamp,
    Plant,
    CoffeeStation,
    RelaxZone,
}
