using System.Security.Claims;

namespace CoreRentalNet.Host.AiBuilder;

/// <summary>The account behind a request, as this application identifies it.</summary>
/// <remarks>
/// One rule in one place. The guard holds a customer to one run by this value, and the run record names them
/// by a hash of it - two callers asking the same question, so they ask it here rather than each deciding for
/// themselves which claim identifies a person. An account with nothing stable to key on has none, and the
/// callers decide what that means: the guard refuses it, and the record says so.
/// </remarks>
internal static class CustomerKey
{
    /// <summary>The customer's stable identity, or null when the account carries none.</summary>
    public static string? Of(ClaimsPrincipal customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var key = customer.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(key))
        {
            key = customer.Identity?.Name;
        }

        return string.IsNullOrWhiteSpace(key) ? null : key;
    }
}
