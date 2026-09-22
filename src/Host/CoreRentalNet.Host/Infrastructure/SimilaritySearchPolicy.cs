namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// The name of the policy that guards the catalogue's similarityService search, in one place.
/// </summary>
/// <remarks>
/// The name is also the configuration section the permission's claim is read from
/// (<c>Authorization:SimilaritySearch</c>), so the endpoint and the deployment refer to one thing. It is a
/// second policy rather than the AI builder's: the two capabilities are asked for separately, and a deployment
/// may allow one and not the other.
/// </remarks>
internal static class SimilaritySearchPolicy
{
    public const string Name = "SimilaritySearch";
}
