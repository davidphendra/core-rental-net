using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;

namespace CoreRentalNet.Evaluation.Datasets.Model;

/// <summary>A source row the loader refused to turn into a case, with the reason it was refused.</summary>
/// <remarks>
/// An excluded row is reported rather than silently dropped: a dataset that quietly shrank between runs would
/// change every rate in the report without anyone noticing.
/// </remarks>
public sealed record DatasetExclusion(string Id, string Reason);
