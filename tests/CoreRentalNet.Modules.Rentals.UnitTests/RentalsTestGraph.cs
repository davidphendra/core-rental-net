using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Rentals.Application.Services;
using CoreRentalNet.Modules.Rentals.Application.Rules;

namespace CoreRentalNet.Modules.Rentals.UnitTests;

/// <summary>The real services, wired the way the composition root wires them.</summary>
internal static class RentalsTestGraph
{
    public static IMoneyService Money { get; } = new MoneyService();

    public static IOpaqueTokenService Tokens { get; } = new OpaqueTokenService();

    public static IRenewalPolicyService Renewals { get; } = new RenewalPolicyService();

    public static IDeliveryPolicyService Deliveries { get; } = new DeliveryPolicyService();

    public static ICancellationPolicyService Cancellations { get; } = new CancellationPolicyService(Renewals);

    public static IRentalLifecycleService Lifecycle { get; } =
        new RentalLifecycleService(Money, Renewals, Deliveries, Cancellations);

    public static IInvoiceService Invoicing { get; } = new InvoiceService(Money, Renewals, Lifecycle);
}
