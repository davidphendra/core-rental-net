using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Domain;

/// <summary>How soon after an order the setup is scheduled to arrive.</summary>
public static class DeliveryPolicy
{
    public const int LeadTimeDays = 2;

    public static DateOnly ScheduledFor(DateOnly placedOn) => placedOn.AddDays(LeadTimeDays);

    public static void EnsureSchedulable(DateOnly placedOn, DateOnly forDate)
    {
        if (forDate < placedOn)
        {
            throw new DomainRuleViolationException($"A delivery cannot be scheduled before the order was placed ({placedOn}).");
        }
    }
}
