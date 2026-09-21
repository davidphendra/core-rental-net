namespace CoreRentalNet.CatalogIngestion;

/// <summary>Finds the tool's project directory from wherever the program happens to be running.</summary>
/// <remarks>
/// <para>
/// Walking up to the project file is what makes a run launched from <c>bin</c> read the same <c>App_Data</c>
/// and the same <c>products.json</c> as a run launched from the project. A published copy has no project file
/// above it, so the base directory is the honest fallback there.
/// </para>
/// <para>
/// <b>It is its own type because it is a filesystem side effect.</b> The configuration reader resolves paths
/// against a directory it is handed rather than finding one itself, so the reader stays a pure function of its
/// inputs and a test can give it any directory it likes.
/// </para>
/// </remarks>
internal static class ProjectDirectory
{
    private const string ProjectFile = "CoreRentalNet.CatalogIngestion.csproj";

    /// <summary>The directory holding this project's file, or the base directory when there is none.</summary>
    public static string Resolve()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, ProjectFile)))
            {
                return directory.FullName;
            }
        }

        return AppContext.BaseDirectory;
    }
}
