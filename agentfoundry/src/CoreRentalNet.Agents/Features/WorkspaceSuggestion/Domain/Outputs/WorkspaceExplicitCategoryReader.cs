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
            .Where(entry => entry.Category == WorkspaceComponentCategory.Chair
                ? ContainsChair(customerQuery)
                : entry.Words.Any(word => ContainsWord(customerQuery, word)))
            .Select(entry => entry.Category)
            .ToHashSet();
    }

    public static WorkspaceComponentCategory? CategoryNearestTo(string customerQuery, int index)
    {
        ArgumentNullException.ThrowIfNull(customerQuery);

        return ExplicitWords
            .SelectMany(entry => entry.Words.Select(word => (
                entry.Category,
                Distance: DistanceTo(
                    entry.Category == WorkspaceComponentCategory.Chair
                        ? WithoutBeanBagChairs(customerQuery)
                        : customerQuery,
                    word,
                    index))))
            .Where(candidate => candidate.Distance >= 0)
            .OrderBy(candidate => candidate.Distance)
            .Select(candidate => (WorkspaceComponentCategory?)candidate.Category)
            .FirstOrDefault();
    }

    private static int DistanceTo(string text, string word, int index)
    {
        var match = Regex.Match(
            text,
            $@"\b{Regex.Escape(word)}\b",
            RegexOptions.IgnoreCase);

        var best = -1;

        while (match.Success)
        {
            // A product name is followed by its category noun ("Dell ... monitor"). Prefer a following noun so a
            // preceding category in a list ("chair, and a Dell ...") does not capture the model number.
            var distance = Math.Abs(match.Index - index) + (match.Index < index ? 1_000 : 0);
            best = best < 0 ? distance : Math.Min(best, distance);
            match = match.NextMatch();
        }

        return best;
    }

    private static bool ContainsWord(string text, string word)
        => Regex.IsMatch(text, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase);

    private static bool ContainsChair(string text)
        => Regex.IsMatch(
            WithoutBeanBagChairs(text),
            @"\b(chair|stool)\b",
            RegexOptions.IgnoreCase);

    private static string WithoutBeanBagChairs(string text)
        => Regex.Replace(
            text,
            @"\b(?:bean\s*bag|beanbag|bean-bag|pouf)\s+chairs?\b",
            string.Empty,
            RegexOptions.IgnoreCase);
}
