namespace CoreRentalNet.Host.Infrastructure;

/// <summary>What a permission decides when the deployment has no identity provider at all.</summary>
/// <remarks>
/// An application whose identity is optional has to answer this for every permission it declares, and the
/// answer is not the same for all of them. Reading the catalogue costs nothing and refusing would make a
/// demonstration that boots without configuration unusable, so it opens. A feature that spends money must
/// not: a deployment that has not been told which claim entitles a caller has not been finished, and the
/// safe reading of an unfinished rule is nobody.
///
/// It is a required argument rather than a default, so that a permission cannot inherit the permissive
/// answer by being written carelessly — the compiler asks the question at every construction site.
///
/// <b>Closed is declared first on purpose.</b> A default-constructed or zero-initialised value is therefore
/// the refusing one, so even the accident this enum exists to prevent falls the safe way.
/// </remarks>
internal enum UnconfiguredBehaviour
{
    /// <summary>Allow nobody, because the rule that would allow someone has not been configured.</summary>
    Closed,

    /// <summary>Allow everyone, because there is nobody to check.</summary>
    Open,
}
