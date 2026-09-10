using System.Security.Claims;
using System.Text.Json.Serialization;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// Who is signed in, as the interactive tree needs to know.
/// </summary>
/// <remarks>
/// Passed into the circuit as a component parameter from the root component, exactly like the
/// draft token, because a value declared by a statically-rendered parent does not cross into an
/// interactive render boundary.
/// <para>
/// A parameter that crosses that boundary is round-tripped through JSON, so this type has to be
/// one the serializer can rebuild: a public constructor whose parameters match its properties, and
/// nothing computed being mistaken for state. An earlier version had a private constructor and the
/// circuit silently refused to start — the page prerendered perfectly and then never became
/// interactive.
/// </para>
/// <para>
/// Claims are read by their standard names first and by their short OIDC names second: the
/// identity provider may or may not map inbound claims, and a customer whose email is silently
/// missing is worse than one the code has to look for.
/// </para>
/// </remarks>
public sealed record CurrentCustomer(string? Subject, string? Email, string? Name)
{
    public static CurrentCustomer Anonymous { get; } = new(null, null, null);

    [JsonIgnore]
    public bool IsSignedIn => !string.IsNullOrWhiteSpace(Subject);

    /// <summary>What is drawn instead of the provider's picture, so no page reaches a third party.</summary>
    [JsonIgnore]
    public string DisplayName => Name ?? Email ?? "Account";

    [JsonIgnore]
    public string Initials
    {
        get
        {
            var source = Name ?? Email ?? string.Empty;
            var words = source.Split([' ', '.', '@', '_', '-'], StringSplitOptions.RemoveEmptyEntries);

            return words.Length switch
            {
                0 => "?",
                1 => words[0][..1].ToUpperInvariant(),
                _ => string.Concat(words[0][..1], words[1][..1]).ToUpperInvariant(),
            };
        }
    }

    public static CurrentCustomer FromClaims(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return Anonymous;
        }

        return new CurrentCustomer(
            Read(principal, ClaimTypes.NameIdentifier, "sub"),
            Read(principal, ClaimTypes.Email, "email"),
            Read(principal, ClaimTypes.Name, "name", "nickname"));
    }

    private static string? Read(ClaimsPrincipal principal, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirst(claimType)?.Value;

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
