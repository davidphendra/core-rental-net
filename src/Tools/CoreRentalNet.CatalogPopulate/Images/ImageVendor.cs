namespace CoreRentalNet.CatalogPopulate.Images;

/// <summary>Downloads each product's image beside its SKU, and removes files whose product is gone.</summary>
internal sealed class ImageVendor(HttpClient client, string directory)
{
    /// <summary>Downloads missing images and deletes orphans, returning (downloaded, kept, removed).</summary>
    public async Task<(int Downloaded, int Kept, int Removed)> VendorAsync(
        IReadOnlyDictionary<string, string?> imageBySku,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(directory);

        var downloaded = 0;
        var kept = 0;

        foreach (var (sku, url) in imageBySku)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            var extension = Extension(url);
            var target = Path.Combine(directory, sku + extension);

            if (File.Exists(target))
            {
                kept++;
                continue;
            }

            var bytes = await client.GetByteArrayAsync(url, cancellationToken);
            await File.WriteAllBytesAsync(target, bytes, cancellationToken);
            downloaded++;
        }

        var removed = RemoveOrphans(imageBySku.Keys);

        return (downloaded, kept, removed);
    }

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
