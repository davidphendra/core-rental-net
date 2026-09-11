// Remembered for the session: no Max-Age and no Expires, so the browser drops this when it closes.
// The browser writes it rather than the server because the click happens inside an interactive
// circuit, where there is no HTTP response to put a cookie on. App.razor reads it back on the next
// request, so the page is never rendered one category and swapped for another.
window.coreRental = window.coreRental || {};

window.coreRental.rememberStoreCategory = function (name) {
    document.cookie = 'store-category=' + encodeURIComponent(name) + '; path=/; SameSite=Lax';
};
