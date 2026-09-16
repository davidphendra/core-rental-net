namespace CoreRentalNet.BuildingBlocks.Application;

/// <summary>
/// The line-art an enum member is drawn with.
/// </summary>
/// <remarks>
/// The member's <see cref="GlyphAttribute"/> when it carries one, and the member's own name
/// otherwise. Separate from <see cref="EnumLabel"/> because a member carries only one
/// <see cref="System.ComponentModel.DescriptionAttribute"/>, and a label and a glyph are two facts
/// rather than one.
/// </remarks>
public static class EnumGlyph
{
    /// <summary>The glyph for this member, or its own name when it carries no glyph.</summary>
    public static string Glyph<TEnum>(this TEnum value)
        where TEnum : struct, Enum
        => EnumAttributes<TEnum, GlyphAttribute>.Read(value, attribute => attribute.Name);
}
