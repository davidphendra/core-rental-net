using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Validation;

namespace CoreRentalNet.Evaluation.Datasets.Loading;

/// <summary>Reads one dataset from disk without modifying it.</summary>
public interface IDatasetLoader
{
    Task<RagEvaluationDataset> LoadAsync(
        DatasetName name,
        string path,
        CancellationToken cancellationToken = default);
}
