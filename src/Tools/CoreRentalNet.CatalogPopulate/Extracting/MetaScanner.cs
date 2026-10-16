using CoreRentalNet.CatalogPopulate.Reporting;
using CoreRentalNet.CatalogPopulate.Selection;
using CoreRentalNet.CatalogPopulate.Sources;

namespace CoreRentalNet.CatalogPopulate.Extracting;

/// <summary>Reads the four meta files once, matching each accepted row to its category, and reports the scan.</summary>
internal sealed class MetaScanner(IProgressReporter reporter)
{
    private const string Step = "Scanning meta sources";

    /// <summary>Rows per category, in the order the four files were read.</summary>
    public Dictionary<string, List<AmazonMetaRecord>> Scan(IReadOnlyList<(string FileKey, string Path)> files)
    {
        var byCategory = ProductCategories.All
            .ToDictionary(category => category.Key, _ => new List<AmazonMetaRecord>(), StringComparer.Ordinal);
        var total = Total(files);
        var scanned = 0L;
        var matched = 0;

        for (var index = 0; index < files.Count; index++)
        {
            var (fileKey, path) = files[index];
            var start = scanned;

            foreach (var row in JsonLineReader.Read<AmazonMetaRecord>(path, Throttle(index + 1, files, path, start, total, () => matched)))
            {
                if (string.IsNullOrWhiteSpace(row.ParentAsin) || string.IsNullOrWhiteSpace(row.Title))
                {
                    continue;
                }

                var category = ProductCategories.Match(row);
                if (category is not null && category.MetaFileKey == fileKey)
                {
                    byCategory[category.Key].Add(row);
                    matched++;
                }
            }

            scanned = start + new FileInfo(path).Length;
            Report(index + 1, files, path, scanned, total, matched);
        }

        return byCategory;
    }

    /// <summary>Reports the scan at most once per percent, so a huge file does not flood the display.</summary>
    private Action<long> Throttle(
        int fileIndex,
        IReadOnlyList<(string FileKey, string Path)> files,
        string path,
        long start,
        long total,
        Func<int> matched)
    {
        var step = Math.Max(1, total / 100);
        var last = 0L;

        return bytes =>
        {
            if (bytes - last < step)
            {
                return;
            }

            last = bytes;
            Report(fileIndex, files, path, start + bytes, total, matched());
        };
    }

    private void Report(
        int fileIndex,
        IReadOnlyList<(string FileKey, string Path)> files,
        string path,
        long scanned,
        long total,
        int matched)
        => reporter.Report(new ProgressUpdate(
            Step,
            scanned,
            total,
            $"file {fileIndex}/{files.Count} · {Path.GetFileName(path)} · {matched} matched"));

    private static long Total(IReadOnlyList<(string FileKey, string Path)> files)
        => Math.Max(1, files.Sum(file => new FileInfo(file.Path).Length));
}
