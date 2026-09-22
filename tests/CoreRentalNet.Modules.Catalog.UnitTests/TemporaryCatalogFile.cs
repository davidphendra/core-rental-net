namespace CoreRentalNet.Modules.Catalog.UnitTests;

/// <summary>A throwaway JSON file, so a repository test needs no fixture directory.</summary>
internal sealed class TemporaryCatalogFile : IDisposable
{
    public TemporaryCatalogFile(string contents)
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"core-rental-catalogService-{Guid.NewGuid():N}.json");
        File.WriteAllText(Path, contents);
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
