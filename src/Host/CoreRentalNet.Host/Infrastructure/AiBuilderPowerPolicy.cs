namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// The name of the policy that decides whether an account is offered every candidate or only one.
/// </summary>
/// <remarks>
/// A separate grant from <see cref="AiBuilderReadPolicy"/>, and it implies nothing: an account holding
/// this without the read permission sees no section at all. Two ways to be entitled to one thing is one
/// way to be entitled for the wrong reason.
/// </remarks>
internal static class AiBuilderPowerPolicy
{
    public const string Name = "AiBuilderPower";
}
