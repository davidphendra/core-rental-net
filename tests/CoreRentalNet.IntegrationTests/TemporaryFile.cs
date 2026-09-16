namespace CoreRentalNet.IntegrationTests;

internal sealed class TemporaryFile : IDisposable
{
    public TemporaryFile(string contents)
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"core-rental-{Guid.NewGuid():N}.json");
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
