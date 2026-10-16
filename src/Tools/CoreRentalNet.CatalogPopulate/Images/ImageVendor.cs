using CoreRentalNet.CatalogPopulate.Reporting;

namespace CoreRentalNet.CatalogPopulate.Images;

/// <summary>Downloads each product's image beside its SKU, and removes files whose product is gone.</summary>
internal sealed class ImageVendor(HttpClient client, string directory)
{
    private const string Step = "Vendoring images";

    /// <summary>Downloads missing images and deletes orphans, returning (downloaded, kept, removed).</summary>
    public async Task<(int Downloaded, int Kept, int Removed)> VendorAsync(
        IReadOnlyDictionary<string, string?> imageBySku,
        IProgressReporter reporter,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(directory);

        var total = imageBySku.Count;
        var done = 0;
        var downloaded = 0;
        var kept = 0;

        foreach (var (sku, url) in imageBySku)
        {
            done++;

            if (string.IsNullOrWhiteSpace(url))
            {
                Report(reporter, done, total, sku);
                continue;
            }

            var extension = Extension(url);
            var target = Path.Combine(directory, sku + extension);

            if (File.Exists(target))
            {
                kept++;
                Report(reporter, done, total, sku);
                continue;
            }

            var bytes = await client.GetByteArrayAsync(url, cancellationToken);
            await File.WriteAllBytesAsync(target, bytes, cancellationToken);
            downloaded++;
            Report(reporter, done, total, sku);
        }

        var removed = RemoveOrphans(imageBySku.Keys);
        reporter.Report(new ProgressUpdate(
            Step, total, total, $"downloaded {downloaded} · kept {kept} · removed {removed}"));

        return (downloaded, kept, removed);
    }

    private static void Report(IProgressReporter reporter, int done, int total, string sku)
        => reporter.Report(new ProgressUpdate(Step, done, total, sku));

    private static string Extension(string url)
    {
        var extension = Path.GetExtension(new Uri(url).AbsolutePath);

        return string.IsNullOrWhiteSpace(extension) ? ".jpg" : extension;
    }

    private int RemoveOrphans(IEnumerable<string> skus)
    {
        var known = new HashSet<string>(skus, StringComparer.Ordinal);
        var removed = 0;

        foreach (var file in Directory.EnumerateFiles(directory))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (!known.Contains(name))
            {
                File.Delete(file);
                removed++;
            }
        }

        return removed;
    }
}
