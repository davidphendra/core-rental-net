using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Application.Rentals;

/// <summary>How soon after an order the setup is scheduled to arrive.</summary>
public interface IDeliveryPolicyService
{
    int LeadTimeDays { get; }

    DateOnly ScheduledFor(DateOnly placedOn);

    void EnsureSchedulable(DateOnly placedOn, DateOnly forDate);
}
