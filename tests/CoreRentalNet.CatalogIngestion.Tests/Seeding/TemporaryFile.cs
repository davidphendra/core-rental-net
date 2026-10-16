namespace CoreRentalNet.CatalogIngestion.Tests.Seeding;

/// <summary>A file that removes itself, for the tests that write one.</summary>
internal sealed class TemporaryFile : IDisposable
{
    public TemporaryFile()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"catalog-ingestion-{Guid.NewGuid():N}.cs");
    }

    public string Path { get; }

    public void Dispose()
    {
        if (File.Exists(Path))
        {
            File.Delete(Path);
        }
    }
}
