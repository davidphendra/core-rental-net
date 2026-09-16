using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Infrastructure;

/// <summary>How often the time-driven run happens. Configuration, because a demo wants it sooner.</summary>
public sealed record SchedulerSettings(TimeSpan Interval)
{
    public static SchedulerSettings FromMinutes(int minutes)
    {
        if (minutes < 1)
        {
            throw new DomainRuleViolationException($"The scheduler interval must be at least a minute, but {minutes} was given.");
        }

        return new SchedulerSettings(TimeSpan.FromMinutes(minutes));
    }
}
