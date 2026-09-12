using CoreRentalNet.Host.Controllers;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// The address that starts a sign-in, with somewhere to come back to.
/// </summary>
/// <remarks>
/// It exists so that the header's link and the gate that sends a guest there cannot drift apart, and
/// so that the return address is sanitised in one place: it arrives from a request, and a return
/// address that leaves this application is an open redirect.
/// </remarks>
internal static class SignInUrl
{
    public static string For(string? returnUrl)
        => $"{AccountController.SignInPath}?returnUrl={Uri.EscapeDataString(LocalUrl.Sanitise(returnUrl))}";
}
