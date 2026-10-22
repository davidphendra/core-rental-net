using System.Text.RegularExpressions;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;

/// <summary>Finds the categories a customer named with a catalogue noun rather than an inference.</summary>
/// <remarks>
/// The compact answer omits irrelevant categories, so an omission is normally correct. A customer who says
/// "desk" or "plant" has named a category explicitly, though, and that category must not disappear because a model
/// treated it as implied by a room type. Only the catalogue's own nouns are read here; room types and adjectives
/// never add a category.
/// </remarks>
internal static class WorkspaceExplicitCategoryReader
{
    private static readonly (WorkspaceComponentCategory Category, string[] Words)[] ExplicitWords =
    [
        (WorkspaceComponentCategory.Desk, ["desk"]),
        (WorkspaceComponentCategory.Chair, ["chair", "stool"]),
        (WorkspaceComponentCategory.Monitor, ["monitor", "screen", "display"]),
        (WorkspaceComponentCategory.Lamp, ["lamp", "lighting", "light"]),
        (WorkspaceComponentCategory.Plant, ["plant", "succulent", "greenery", "tree"]),
        (WorkspaceComponentCategory.BeanBag, ["bean bag", "beanbag", "bean-bag"]),
        (WorkspaceComponentCategory.CoffeeMachine,
            ["coffee machine", "coffee maker", "espresso", "coffee station"]),
    ];

    public static IReadOnlySet<WorkspaceComponentCategory> Read(string customerQuery)
    {
        ArgumentNullException.ThrowIfNull(customerQuery);

        return ExplicitWords
            .Where(entry => entry.Words.Any(word => ContainsWord(customerQuery, word)))
            .Select(entry => entry.Category)
            .ToHashSet();
    }

    private static bool ContainsWord(string text, string word)
        => Regex.IsMatch(text, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase);
}
