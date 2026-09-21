namespace CoreRentalNet.Modules.Discovery.Application.Selection;

/// <summary>Records that a candidate was applied, so what customers actually take can rank future shortlists.</summary>
/// <remarks>
/// <para>
/// <b>One operation, and it is the only thing that writes a chosen count.</b> The alternative signal — counting
/// every option the model proposed — was refused because a proposal is not an endorsement: an option that was
/// displayed and ignored is evidence of nothing, and an option that failed validation was never displayed at
/// all. Applied is the one unambiguous act a customer performs.
/// </para>
/// <para>
/// It returns how many products were credited rather than nothing, so a caller can log a number and a test can
/// hold it to what it was given. A failure throws, and the caller decides what that means: at apply time the
/// composition is already written, so a signal that cannot be recorded is a fact to log rather than a reason to
/// undo a customer's choice.
/// </para>
/// </remarks>
public interface ICreditSelection
{
    /// <summary>Credits each distinct SKU once, and returns how many were credited.</summary>
    Task<int> CreditAsync(IReadOnlyList<string> skus, CancellationToken cancellationToken);
}
