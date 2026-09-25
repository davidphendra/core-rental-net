using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CoreRentalNet.Host.AiBuilder;

/// <summary>The account behind a request, as this application identifies it, and the opaque id it becomes.</summary>
/// <remarks>
/// One rule in one place: which claim identifies a person is asked here rather than decided again at each call
/// site. An account with nothing stable to identify it has none, and the caller says so rather than inventing
/// one.
/// </remarks>
internal static class CustomerKey
{
    /// <summary>What the run record names the customer by, and never the account itself.</summary>
    /// <remarks>
    /// A hash, because the record is a log line and the account is not the log's to hold - the same shape this
    /// application already stores its own token hashes in. It is computed here rather than injected: this is not
    /// a token, and the service that issues tokens is not the run record's dependency.
    /// </remarks>
    public static string HashedOf(ClaimsPrincipal customer)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Of(customer) ?? "anonymous")));

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
