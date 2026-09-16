using System.ComponentModel;

namespace CoreRentalNet.BuildingBlocks.Application;

/// <summary>
/// What an enum member is called where a person reads it.
/// </summary>
/// <remarks>
/// The member's <see cref="DescriptionAttribute"/> when it has one, and the member's own name
/// otherwise, so a vocabulary annotates only the members whose label is not their identifier.
/// The twin of <see cref="EnumGlyph"/>.
/// </remarks>
public static class EnumLabel
{
    /// <summary>The label to show for this member.</summary>
    public static string Label<TEnum>(this TEnum value)
        where TEnum : struct, Enum
        => EnumAttributes<TEnum, DescriptionAttribute>.Read(value, attribute => attribute.Description);
}
