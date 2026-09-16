using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Application.Checkout;

/// <summary>
/// The typed acknowledgement that stands in for payment.
/// </summary>
/// <remarks>
/// There is no payment provider in this application, so the deliberate act of typing
/// the phrase is the whole of checkout. It is checked in the browser to keep the confirm control
/// disabled, and checked again here because a disabled button is a courtesy, never a boundary.
/// Matching is trimmed and case-insensitive, otherwise exact: a demonstration should not fail on a
/// trailing space or a capital letter.
/// </remarks>
public static class DemoConfirmation
{
    public const string Phrase = "this is a demo";

    public static bool IsSatisfied(string? typed)
        => typed is not null && string.Equals(typed.Trim(), Phrase, StringComparison.OrdinalIgnoreCase);

    public static void EnsureSatisfied(string? typed)
    {
        if (!IsSatisfied(typed))
        {
            throw new DomainRuleViolationException(
                $"Type '{Phrase}' to confirm that this is a demonstration.");
        }
    }
}
