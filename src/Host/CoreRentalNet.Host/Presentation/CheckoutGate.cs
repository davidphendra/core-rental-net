using CoreRentalNet.Modules.Workspace.Application.Queries.Views;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// Whether a workspace can be ordered, and why not when it cannot.
/// </summary>
/// <remarks>
/// <para>
/// The domain decides whether a workspace can be rented: a desk and a chair, and nothing in it that
/// has left the catalogService. The order itself also refuses a blank delivery address, which is asked for
/// on the review page - so this is the two together, and it belongs to that button rather than to
/// <see cref="WorkspaceView.CanCheckout"/>.
/// </para>
/// <para>
/// That distinction is not academic. If the address gated the workspace instead, the floating bar,
/// the bag in the header and the phone's Rent button would all close on a workspace with no address -
/// and there would be no way left to reach the page where the address is entered. A rule that
/// refuses one step further along is a rule you can still satisfy.
/// </para>
/// </remarks>
public static class CheckoutGate
{
    /// <summary>What the button says when the address is the only thing missing.</summary>
    public const string AddressRequired = "Enter a delivery address before you rent.";

    /// <summary>What the button says when there is no workspace to rent yet.</summary>
    public const string WorkspaceRequired = "Add a desk and a chair to rent your workspace.";

    /// <param name="typedAddress">
    /// What is in the field right now, which may not be saved yet: the address is written to the draft
    /// when the field loses focus, so counting only what is stored would leave the button shut until
    /// something blurred a field the customer had not finished with.
    /// </param>
    public static bool CanPlaceOrder(WorkspaceView? workspace, string? typedAddress = null)
        => workspace is { CanCheckout: true } && HasAddress(workspace, typedAddress);

    /// <summary>The reason the order cannot be placed, or null when it can.</summary>
    public static string? BlockingReason(WorkspaceView? workspace, string? typedAddress = null) => workspace switch
    {
        null => WorkspaceRequired,

        // The workspace's own answer first: an address is worth nothing without a desk and a chair.
        _ when !workspace.CanCheckout => workspace.BlockingReason,
        _ when !HasAddress(workspace, typedAddress) => AddressRequired,
        _ => null,
    };

    private static bool HasAddress(WorkspaceView workspace, string? typedAddress)
        => !string.IsNullOrWhiteSpace(typedAddress) || !string.IsNullOrWhiteSpace(workspace.DeliveryAddress);
}
