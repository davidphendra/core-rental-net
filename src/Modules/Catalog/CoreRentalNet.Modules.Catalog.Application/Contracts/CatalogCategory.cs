using System.ComponentModel;
using CoreRentalNet.BuildingBlocks.Application;

namespace CoreRentalNet.Modules.Catalog.Application.Contracts;

/// <summary>
/// The category vocabulary this module publishes to its callers.
/// </summary>
/// <remarks>
/// Deliberately separate from <c>Catalog.Domain.ProductCategory</c>: no other module has to
/// reference this module's Domain, which the architecture tests enforce. The two vocabularies meet
/// in one mapper, inside this module.
/// The member's <see cref="DescriptionAttribute"/> is what a person reads - a label is plural, an
/// identifier is not - and <c>EnumLabel.Label()</c> returns it. Its <see cref="GlyphAttribute"/> is
/// the line-art drawn beside that word, which <c>EnumGlyph.Glyph()</c> returns.
/// </remarks>
public enum CatalogCategory
{
    [Description("Chairs")]
    [Glyph("chair")]
    Chair = 1,

    [Description("Desks")]
    [Glyph("desk")]
    Desk = 2,

    [Description("Accessories")]
    [Glyph("keyboard")]
    Accessory = 3,
}
