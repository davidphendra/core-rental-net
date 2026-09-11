namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// Keeps the login page from becoming an open redirect.
/// </summary>
/// <remarks>
/// This is the framework's own rule: a local URL starts with a single forward slash, and not with
/// two, and not with a backslash that a browser might treat as one.
/// </remarks>
internal static class LocalUrl
{
    public static string Sanitise(string? candidate, string fallback = "/")
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return fallback;
        }

        var url = candidate.Trim();

        return IsLocal(url) ? url : fallback;
    }

    public static bool IsLocal(string? url)
        => !string.IsNullOrEmpty(url)
           && url[0] == '/'
           && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
}
