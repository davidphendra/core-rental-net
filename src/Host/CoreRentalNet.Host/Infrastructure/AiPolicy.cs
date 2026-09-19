namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// The name of the policy that guards the AI workspace builder, in one place.
/// </summary>
/// <remarks>
/// The name is also the configuration section the permission's claim is read from
/// (<c>Authorization:AIUse</c>), so the page, the endpoint and the deployment all refer to one thing.
/// </remarks>
internal static class AiPolicy
{
    public const string Name = "AIUse";
}
