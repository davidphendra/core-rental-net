using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;

namespace CoreRentalNet.Evaluation.Datasets.Validation;

/// <summary>What the validator found, kept as text so a failing dataset names the case that broke it.</summary>
public sealed record DatasetValidation(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings)
{
    public static DatasetValidation Valid(IReadOnlyList<string>? warnings = null)
        => new(true, [], warnings ?? []);

    public static DatasetValidation Invalid(IReadOnlyList<string> errors, IReadOnlyList<string>? warnings = null)
        => new(false, errors, warnings ?? []);
}
