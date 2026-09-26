using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>The account behind a request, as this application identifies it, and the opaque id it becomes.</summary>
/// <remarks>
/// One rule in one place: which claim identifies a person is asked here rather than decided again at each call
/// site. A hash, because the run record is a log line and the account is not the log's to hold. An account with
/// nothing stable to identify it has none, and the caller says so rather than inventing one.
/// </remarks>
internal static class ClaimsPrincipalIdentityExtensions
{
    /// <summary>What the run record names the customer by, and never the account itself.</summary>
    public static string HashCustomerIdentity(this ClaimsPrincipal claimsPrincipal)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Of(claimsPrincipal) ?? "anonymous")));

    /// <summary>The customer's stable identity, or null when the account carries none.</summary>
    private static string? Of(ClaimsPrincipal customer)
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
