using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// The browsing state one surface is showing: the category, the search, and the catalogService's answer for
/// them.
/// </summary>
/// <remarks>
/// <para>
/// Behaviour with no rendering in it, so it is a plain class rather than a component base and can be
/// tested without a renderer. Each surface owns its own instance: the docked panel's browsing has
/// nothing to do with the store's, which is how these fields - and the rule that choosing a category
/// empties the search - came to be written twice.
/// </para>
/// <para>
/// The catalogService is asked through <see cref="CatalogPageQuery"/> rather than a handler, so a test can
/// answer without a catalogService; the composition supplies the real one.
/// </para>
/// </remarks>
public sealed class CatalogBrowser
{
    private readonly CatalogPageQuery _ask;

    public CatalogBrowser(CatalogPageQuery ask)
    {
        ArgumentNullException.ThrowIfNull(ask);

        _ask = ask;
    }

    /// <summary>Which category is being shown.</summary>
    public CatalogTab Tab { get; private set; } = CatalogTab.Desks;

    /// <summary>What the customer typed, or the empty string.</summary>
    public string Query { get; private set; } = string.Empty;

    /// <summary>The catalogService's answer for the tab and the search.</summary>
    public IReadOnlyList<ProductView> Items { get; private set; } = [];

    /// <summary>
    /// Chooses a category and empties the search: a search that matched nothing in the last tab is
    /// not allowed to hide this one's products.
    /// </summary>
    public void SelectTab(CatalogTab tab)
    {
        Tab = tab;
        Query = string.Empty;

        Reload();
    }

    /// <summary>The one way the search changes: typed into a field, or cleared by its control.</summary>
    public void Search(string? query)
    {
        Query = query ?? string.Empty;

        Reload();
    }

    /// <summary>Asks the catalogService again for what the tab and the search now describe.</summary>
    public void Reload() => Items = _ask(Tab, Query);
}

/// <summary>How the browser asks the catalogService for what one tab and one search describe.</summary>
public delegate IReadOnlyList<ProductView> CatalogPageQuery(CatalogTab tab, string search);
