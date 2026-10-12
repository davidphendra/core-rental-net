namespace CoreRentalNet.CatalogPopulate;

/// <summary>Finds the tool's project directory from wherever the program happens to be running.</summary>
internal static class ProjectDirectory
{
    private const string ProjectFile = "CoreRentalNet.CatalogPopulate.csproj";

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
