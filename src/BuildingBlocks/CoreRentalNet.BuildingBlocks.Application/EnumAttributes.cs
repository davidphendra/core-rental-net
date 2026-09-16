using System.Collections.Frozen;
using System.Reflection;

namespace CoreRentalNet.BuildingBlocks.Application;

/// <summary>
/// What one kind of attribute says about each member of an enum, read once per enum type.
/// </summary>
/// <remarks>
/// Enum.ToString is documented as expensive because it reflects over the type on every call, and a
/// search or a render reads a word per item, so the lookup is built once and never written again.
/// A member that carries no attribute of this kind is absent from the lookup rather than an error,
/// which is what lets a vocabulary annotate only the members that need annotating.
/// </remarks>
internal static class EnumAttributes<TEnum, TAttribute>
    where TEnum : struct, Enum
    where TAttribute : Attribute
{
    private static readonly FrozenDictionary<TEnum, TAttribute?> s_attributesByMember = Build();

    /// <summary>The text this member's attribute holds, or the member's own name when there is none.</summary>
    public static string Read(TEnum member, Func<TAttribute, string?> readText)
        => s_attributesByMember.TryGetValue(member, out var attribute)
            && attribute is not null
            && readText(attribute) is { Length: > 0 } value
                ? value
                : member.ToString();

    private static FrozenDictionary<TEnum, TAttribute?> Build()
        => Enum.GetValues<TEnum>().ToFrozenDictionary(
            member => member,
            member => typeof(TEnum).GetField(member.ToString())?.GetCustomAttribute<TAttribute>());
}
