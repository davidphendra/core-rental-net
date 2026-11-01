using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;

namespace CoreRentalNet.Evaluation.Datasets.Model;

/// <summary>Which independent dataset a run reads. Test is the lockbox and is gated at the command line.</summary>
public enum DatasetName
{
    Development,
    Validation,
    Test,
}
