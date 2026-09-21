using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Discovery.Application.Shortlist;

/// <summary>
/// One of the catalogue's own groups: a category, and the subcategory that narrows it.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the catalogue's published vocabulary and not the workspace's.</b> The slots live in
/// <c>Workspace.Domain</c>, and a module may not reach another module's Domain (ARC-01) — so a shortlist
/// expressed in <c>SlotId</c> was never available here, and mirroring the type would have created a second
/// copy of a vocabulary this repository has already been bitten by twice.
/// </para>
/// <para>
/// Using the catalogue's vocabulary is only sound because the two partitions are provably the same: the seven
/// <c>(category, subCategory)</c> pairs the real catalogue holds map to seven distinct slots, which is asserted
/// rather than assumed (<c>CatalogSlotCoverageTests</c>). If that ever stops being true, the equivalence is
/// gone and the buckets have to be expressed some other way.
/// </para>
/// </remarks>
public sealed record CatalogBucket(CatalogCategory Category, CatalogSubCategory? SubCategory);
