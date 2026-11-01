namespace CoreRentalNet.Evaluation.Tests.Fixtures;

/// <summary>Finds the repository root from the test output directory.</summary>
internal static class EvaluationRepoRoot
{
    public static string Path { get; } = Find();

    private static string Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(System.IO.Path.Combine(
                    directory.FullName,
                    "evaluation",
                    "src",
                    "CoreRentalNet.Evaluation",
                    "CoreRentalNet.Evaluation.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "The repository root could not be found from the evaluation test output directory.");
    }
}
