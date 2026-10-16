using CoreRentalNet.CatalogPopulate.Compact;
using CoreRentalNet.CatalogPopulate.Reporting;
using CoreRentalNet.CatalogPopulate.Sources;

namespace CoreRentalNet.CatalogPopulate.Extracting;

/// <summary>Reads the four review files once, keeping only the reviews of the selected products.</summary>
internal sealed class ReviewReader(IProgressReporter reporter)
{
    private const string Step = "Reading reviews";

    /// <summary>Reviews per selected product, keyed by the source id.</summary>
    public Dictionary<string, List<CompactReview>> Read(
        IReadOnlyList<(string FileKey, string Path)> files,
        IEnumerable<string> asins)
    {
        var wanted = new HashSet<string>(asins, StringComparer.Ordinal);
        var reviews = wanted.ToDictionary(asin => asin, _ => new List<CompactReview>(), StringComparer.Ordinal);
        var total = Total(files);
        var scanned = 0L;
        var kept = 0;

        for (var index = 0; index < files.Count; index++)
        {
            var (_, path) = files[index];
            var start = scanned;

            foreach (var row in JsonLineReader.Read<AmazonReviewRecord>(path, Throttle(index + 1, files, path, start, total, () => kept)))
            {
                if (row.ParentAsin is not null && wanted.Contains(row.ParentAsin))
                {
                    reviews[row.ParentAsin].Add(
                        new CompactReview(row.Rating, row.Title, row.Text, row.HelpfulVote, row.Timestamp));
                    kept++;
                }
            }

            scanned = start + new FileInfo(path).Length;
            Report(index + 1, files, path, scanned, total, kept);
        }

        return reviews;
    }

    /// <summary>Reports the read at most once per percent, so a huge file does not flood the display.</summary>
    private Action<long> Throttle(
        int fileIndex,
        IReadOnlyList<(string FileKey, string Path)> files,
        string path,
        long start,
        long total,
        Func<int> kept)
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
            Report(fileIndex, files, path, start + bytes, total, kept());
        };
    }

    private void Report(
        int fileIndex,
        IReadOnlyList<(string FileKey, string Path)> files,
        string path,
        long scanned,
        long total,
        int kept)
        => reporter.Report(new ProgressUpdate(
            Step,
            scanned,
            total,
            $"file {fileIndex}/{files.Count} · {Path.GetFileName(path)} · {kept} reviews matched"));

    private static long Total(IReadOnlyList<(string FileKey, string Path)> files)
        => Math.Max(1, files.Sum(file => new FileInfo(file.Path).Length));
}
