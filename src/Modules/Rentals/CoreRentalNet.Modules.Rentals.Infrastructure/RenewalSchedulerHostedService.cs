using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Application.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

/// <summary>
/// Runs the schedule on a timer.
/// </summary>
/// <remarks>
/// The date comes from <see cref="TimeProvider"/> and the wait uses it too, so the whole loop is
/// testable without waiting for real time to pass. Failures are logged and the loop continues: a
/// single order that cannot advance must not stop the rest of the book.
/// </remarks>
public sealed class RenewalSchedulerHostedService(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    SchedulerSettings settings,
    ILogger<RenewalSchedulerHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken).ConfigureAwait(false);

            try
            {
                await Task.Delay(settings.Interval, clock, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var scheduler = scope.ServiceProvider.GetRequiredService<IRunRentalSchedule>();

            var outcome = await scheduler.RunOnceAsync(BusinessTime.Today(clock), stoppingToken).ConfigureAwait(false);

            foreach (var action in outcome.Actions)
            {
                logger.LogInformation("Scheduler {Kind} for {RentalNumber} {Invoice}", action.Kind, action.RentalNumber, action.InvoiceNumber);
            }

            foreach (var failure in outcome.Failures)
            {
                logger.LogWarning("Scheduler could not advance an order: {Failure}", failure);
            }

            if (!outcome.DidNothing)
            {
                logger.LogInformation(
                    "Scheduler pass for {AsOf}: {Scheduled} scheduled, {Activated} activated, {Invoiced} invoiced, {Ended} ended",
                    outcome.AsOf,
                    outcome.CountOf(ScheduleActionKind.DeliveryScheduled),
                    outcome.CountOf(ScheduleActionKind.Activated),
                    outcome.CountOf(ScheduleActionKind.InvoiceIssued),
                    outcome.CountOf(ScheduleActionKind.Ended));
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "A scheduler pass failed and will be retried on the next tick");
        }
    }
}
