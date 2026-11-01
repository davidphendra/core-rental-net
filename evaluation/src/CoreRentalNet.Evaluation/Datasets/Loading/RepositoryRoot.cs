using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Validation;

namespace CoreRentalNet.Evaluation.Datasets.Loading;

/// <summary>Finds the repository root so a dataset path works from a build output directory and from the root.</summary>
/// <remarks>
/// The console runs from its output directory when it is launched by `dotnet run`, but the datasets live beside
/// the solution. Resolving the root once keeps the default paths the same in both cases rather than making the
/// caller pass an absolute path to get a first run.
/// </remarks>
internal static class RepositoryRoot
{
    public static string Find()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var directory = new DirectoryInfo(start);

            while (directory is not null)
            {
                if (IsRoot(directory.FullName))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        return Directory.GetCurrentDirectory();
    }

    private static bool IsRoot(string path)
        => File.Exists(Path.Combine(path, "Test_Script.md"))
            && Directory.Exists(Path.Combine(path, "evaluation", "data"));
}
