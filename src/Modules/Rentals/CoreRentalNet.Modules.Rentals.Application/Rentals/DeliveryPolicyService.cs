using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Application.Rentals;

/// <summary>The delivery lead time, moved off the Domain's static policy.</summary>
public sealed class DeliveryPolicyService : IDeliveryPolicyService
{
    public const int LeadTimeDaysValue = 2;

    public int LeadTimeDays => LeadTimeDaysValue;

    public DateOnly ScheduledFor(DateOnly placedOn) => placedOn.AddDays(LeadTimeDaysValue);

    public void EnsureSchedulable(DateOnly placedOn, DateOnly forDate)
    {
        if (forDate < placedOn)
        {
            throw new DomainRuleViolationException($"A delivery cannot be scheduled before the order was placed ({placedOn}).");
        }
    }
}
