using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Validation;

namespace CoreRentalNet.Evaluation.Datasets.Loading;

/// <summary>Where each dataset lives, overridable from the command line.</summary>
/// <remarks>
/// The files are read in place and never rewritten: a run records their hash instead of copying them, so the
/// lockbox on disk stays the single source of truth.
/// </remarks>
public sealed record DatasetPaths(string Development, string Validation, string Test)
{
    public static DatasetPaths Default { get; } = CreateDefault();

    private static DatasetPaths CreateDefault()
    {
        var root = RepositoryRoot.Find();

        return new DatasetPaths(
            Development: Path.Combine(root, "Test_Script.md"),
            Validation: Path.Combine(root, "evaluation", "data", "workspace-suggestion-validation-cases-100.json"),
            Test: Path.Combine(root, "evaluation", "data", "workspace-suggestion-test-cases-100.json"));
    }

    public string For(DatasetName name) => name switch
    {
        DatasetName.Development => Development,
        DatasetName.Validation => Validation,
        DatasetName.Test => Test,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown dataset name."),
    };
}
