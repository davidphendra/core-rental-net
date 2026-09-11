namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// The store's chosen category, remembered for the session in a cookie.
/// </summary>
/// <remarks>
/// The cookie carries no expiry, which is what makes it a session cookie: the browser drops it when
/// it closes. That is the whole of "remembered for the session" - there is no other store.
///
/// The browser writes it, because a click on a pill happens inside an interactive circuit, where
/// there is no HTTP response to attach a cookie to. The application reads it on the way in, in
/// App.razor, where there is a request - the same place and for the same reason the draft token is
/// read: reading it any later would mean rendering one category and swapping it for another.
/// </remarks>
public static class StoreCategoryPreference
{
    /// <summary>Named in one place, because two processes have to agree on it.</summary>
    public const string CookieName = "store-category";

    /// <summary>The writer, defined in wwwroot/js/store.js.</summary>
    public const string RememberFunction = "coreRental.rememberStoreCategory";

    /// <summary>
    /// The remembered category, or desks when there is nothing remembered or nothing recognisable.
    /// </summary>
    public static CatalogTab Read(IRequestCookieCollection? cookies)
        => CatalogTabs.FromName(cookies?[CookieName]);
}
