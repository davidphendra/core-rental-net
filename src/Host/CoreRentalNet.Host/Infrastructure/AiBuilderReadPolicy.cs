namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// The name of the policy that guards the builder's AI section.
/// </summary>
/// <remarks>
/// A second permission, not a second catalogue policy: the builder already requires
/// <see cref="CatalogPolicy"/>, so this is additive and every account that sees the section holds both.
/// </remarks>
internal static class AiBuilderReadPolicy
{
    public const string Name = "AiBuilderRead";
}
