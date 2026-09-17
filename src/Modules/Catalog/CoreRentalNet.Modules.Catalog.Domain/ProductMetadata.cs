namespace CoreRentalNet.Modules.Catalog.Domain;

/// <summary>
/// What a product says about itself beyond its name: the vocabulary a request can be matched against.
/// </summary>
/// <remarks>
/// A plain record, and typed rather than a free-form bag, because a criterion is generated from it -
/// <c>tag:quiet</c> or <c>attribute:desk:type:standing</c> either resolves to products or it does
/// not. The loader is what refuses a malformed one, and the published mirror in
/// <c>Application.Contracts</c> is what callers read, so this type stays inside the module.
/// </remarks>
public sealed record ProductMetadata(
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> Attributes,
    IReadOnlyList<string> BestFor,
    IReadOnlyList<string> NotFor);
