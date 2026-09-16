namespace CoreRentalNet.BuildingBlocks.Application;

/// <summary>The line-art a member is drawn with, named as the path table knows it.</summary>
/// <param name="name">The name the glyph is known by.</param>
[AttributeUsage(AttributeTargets.Field)]
public sealed class GlyphAttribute(string name) : Attribute
{
    /// <summary>The name the glyph is known by.</summary>
    public string Name { get; } = name;
}
