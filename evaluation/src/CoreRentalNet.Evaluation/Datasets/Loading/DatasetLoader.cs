using System.Security.Cryptography;
using System.Text.Json;
using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;

using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Validation;
namespace CoreRentalNet.Evaluation.Datasets.Loading;

/// <summary>Reads a dataset in place, hashing the exact bytes it handed to the parser.</summary>
/// <remarks>
/// The file is opened read-only and shared for reading only, so a run cannot rewrite the cases it is measuring.
/// The hash covers the bytes on disk rather than the deserialised objects, because equality of objects would hide
/// a whitespace or ordering change that makes two runs incomparable.
/// </remarks>
public sealed class DatasetLoader : IDatasetLoader
{
    public async Task<RagEvaluationDataset> LoadAsync(
        DatasetName name,
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var bytes = await ReadOnlyAsync(path, cancellationToken).ConfigureAwait(false);
        var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var (cases, exclusions) = name == DatasetName.Development
            ? DevelopmentCaseReader.Read(bytes, Path.GetDirectoryName(Path.GetFullPath(path)))
            : (JsonCaseReader.Read(bytes), (IReadOnlyList<DatasetExclusion>)[]);

        return new RagEvaluationDataset(
            Name: name,
            Path: Path.GetFullPath(path),
            Version: SysVersion(cases.Count, hash),
            Sha256: hash,
            SizeInBytes: bytes.LongLength,
            Cases: cases,
            Exclusions: exclusions);
    }

    private static async Task<byte[]> ReadOnlyAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    private static string SysVersion(int caseCount, string hash) => $"sha256:{hash[..12]};cases:{caseCount}";
}
