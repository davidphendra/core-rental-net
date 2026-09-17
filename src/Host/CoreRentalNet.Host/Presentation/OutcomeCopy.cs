using CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// Every word the AI section says, in one place.
/// </summary>
/// <remarks>
/// <para>
/// The agent's contract forbids prose and carries codes, so this is the only place a sentence about a run
/// exists - which is what makes it impossible for anything derived from a model, a query or the catalogue
/// to become part of the interface. The one exception is the customer's own phrase, which the contract
/// carries as text precisely because it is theirs.
/// </para>
/// <para>
/// Nothing here renders a raw code. An unrecognised one is a code the application has not been taught,
/// and printing it would put the agent's vocabulary on the page while looking like a fallback; the
/// fallbacks below are sentences a person could read.
/// </para>
/// </remarks>
internal static class OutcomeCopy
{
    /// <summary>What a run that could not satisfy everything says.</summary>
    public const string Caveat =
        "We could not satisfy every requirement, so these are the closest we could find.";

    /// <summary>
    /// What the application says when it could not reach an agent at all.
    /// </summary>
    /// <remarks>
    /// It says nothing changed, because a customer who has just watched a spinner needs to know whether
    /// their workspace moved. It did not, and this is the outcome where that is worth promising.
    /// </remarks>
    public const string Unreachable =
        "We could not reach the suggestion service. Nothing has changed in your workspace.";

    /// <summary>The control that offers another go, which only this outcome gets.</summary>
    public const string Retry = "Try again";

    /// <summary>
    /// What a customer is told when they ask for a run while one is still going.
    /// </summary>
    /// <remarks>
    /// It says what is happening rather than that something is wrong, because a double click is not a
    /// mistake worth scolding - and it says the first request is still running, so the customer knows to
    /// wait for that one rather than assuming it was lost.
    /// </remarks>
    public const string AlreadyRunning =
        "Your last request is still being worked on. We will show it here as soon as it is ready.";

    private const string RefusedFallback =
        "That does not look like a workspace request. Tell us what you need to furnish and we will put "
        + "options together.";

    private const string TierFallback = "Option";

    private const string CriterionFallback = "something we could not check";

    private static readonly IReadOnlyDictionary<string, string> Refusals =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["not_workspace_request"] = RefusedFallback,
        };

    private static readonly IReadOnlyDictionary<string, string> TierNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["low"] = "Essential",
            ["middle"] = "Balanced",
            ["high"] = "Premium",
        };

    /// <summary>
    /// Why the run refused, in the application's words.
    /// </summary>
    /// <remarks>
    /// There is no retry beside this, and that is the point of the outcome: the same words sent again
    /// produce the same refusal. What helps a customer who typed the wrong thing is the guidance, not
    /// another attempt.
    /// </remarks>
    public static string Refusal(string? code)
        => code is not null && Refusals.TryGetValue(code, out var words) ? words : RefusedFallback;

    /// <summary>The tier as a customer reads it. The wire says low, middle and high.</summary>
    public static string Tier(string? tier)
        => tier is not null && TierNames.TryGetValue(tier, out var words) ? words : TierFallback;

    /// <summary>An objection, placed on the slot it is about.</summary>
    public static string Finding(SuggestionFinding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);

        var slot = Slot(finding.Slot);

        return finding.Kind switch
        {
            "criteria_not_met" => $"We could not meet every requirement you gave for the {slot}.",
            "tier_composition" => $"The {slot} could not be arranged the way you asked.",
            _ => $"Something about the {slot} could not be done as asked.",
        };
    }

    /// <summary>
    /// A criterion the option satisfies, as words rather than as a token.
    /// </summary>
    /// <remarks>
    /// The contract's criteria are tokens - <c>slot:chair</c>, <c>attribute:desk:type:sit-stand</c> - and a
    /// token on a page is the agent's vocabulary leaking. What follows the first colon is the part that
    /// says something, so it is read out with its separators opened up.
    /// </remarks>
    public static string Criterion(string? token)
    {
        var boundary = token?.IndexOf(':', StringComparison.Ordinal) ?? -1;

        if (token is null || boundary < 0 || boundary == token.Length - 1)
        {
            return CriterionFallback;
        }

        return token[(boundary + 1)..].Replace(':', ' ').Replace('-', ' ');
    }

    /// <summary>A slot a finding is about, with the contract's compound names opened up.</summary>
    public static string Slot(string? slot)
        => string.IsNullOrWhiteSpace(slot)
            ? CriterionFallback
            : System.Text.RegularExpressions.Regex.Replace(slot, "(?<=[a-z])(?=[A-Z])", " ").ToLowerInvariant();
}
