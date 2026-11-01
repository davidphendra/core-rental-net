using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;

namespace CoreRentalNet.Evaluation.Datasets.Model;

/// <summary>A loaded dataset and the provenance a run record must carry to be comparable.</summary>
public sealed record RagEvaluationDataset(
    DatasetName Name,
    string Path,
    string Version,
    string Sha256,
    long SizeInBytes,
    IReadOnlyList<RagEvaluationCase> Cases,
    IReadOnlyList<DatasetExclusion> Exclusions)
{
    /// <summary>Whether the dataset is the one-shot lockbox.</summary>
    public bool IsLockbox => Name == DatasetName.Test;
}
