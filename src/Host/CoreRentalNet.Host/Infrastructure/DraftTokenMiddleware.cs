using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// Owns the draft token's lifetime.
/// </summary>
/// <remarks>
/// This lives in middleware rather than in a component because a Blazor Server event handler
/// runs over the SignalR circuit, not in an HTTP response, so a component cannot set a cookie
/// (ADR-0007). The resolved raw token is placed in <c>HttpContext.Items</c> so the root
/// component can hand it to the interactive tree without reading the cookie again.
/// </remarks>
public sealed class DraftTokenMiddleware(RequestDelegate next)
{
    public const string CookieName = "corerental.draft";

    /// <summary>Present in the query string, this rotates the token and redirects without it.</summary>
    public const string RotateQueryKey = "newDraft";

    public const string ItemsKey = "corerental.draft.token";

    private const int CookieLifetimeDays = 30;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var rotates = context.Request.Query.ContainsKey(RotateQueryKey);

        if (rotates)
        {
            Issue(context, DraftToken.IssueRawToken());

            // Redirect so a refresh does not rotate again.
            context.Response.Redirect(StripRotateFlag(context.Request));
            return;
        }

        var raw = context.Request.Cookies[CookieName];

        if (string.IsNullOrWhiteSpace(raw))
        {
            raw = DraftToken.IssueRawToken();
            Issue(context, raw);
        }

        context.Items[ItemsKey] = raw;

        await next(context).ConfigureAwait(false);
    }

    /// <summary>The raw draft token for the current request, or null outside a request.</summary>
    public static string? ResolveToken(HttpContext? httpContext)
        => httpContext?.Items.TryGetValue(ItemsKey, out var value) == true ? value as string : null;

    private static void Issue(HttpContext context, string rawToken)
        => context.Response.Cookies.Append(
            CookieName,
            rawToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromDays(CookieLifetimeDays),
                Path = "/",
            });

    private static string StripRotateFlag(HttpRequest request)
    {
        var kept = request.Query
            .Where(pair => !string.Equals(pair.Key, RotateQueryKey, StringComparison.Ordinal))
            .SelectMany(pair => pair.Value.Select(value => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(value ?? string.Empty)}"))
            .ToArray();

        return kept.Length == 0 ? request.Path : $"{request.Path}?{string.Join('&', kept)}";
    }
}
