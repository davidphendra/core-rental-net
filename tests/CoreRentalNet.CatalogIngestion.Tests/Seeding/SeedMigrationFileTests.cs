using AwesomeAssertions;
using CoreRentalNet.CatalogIngestion.Seeding;
using Xunit;

namespace CoreRentalNet.CatalogIngestion.Tests.Seeding;

/// <summary>Finding the migration shell the writer overwrites.</summary>
public sealed class SeedMigrationFileTests
{
    [Fact]
    public void The_single_shell_is_found()
    {
        using var directory = new TemporaryDirectory();
        var shell = Path.Combine(directory.Path, "20260101000000_SeedProductEmbeddings.cs");
        File.WriteAllText(shell, string.Empty);

        SeedMigrationFile.In(directory.Path).Path.Should().Be(shell);
    }

    [Fact]
    public void No_shell_names_the_command_that_creates_one()
    {
        using var directory = new TemporaryDirectory();

        var act = () => SeedMigrationFile.In(directory.Path);

        act.Should().Throw<InvalidOperationException>().WithMessage("*migrations add SeedProductEmbeddings*");
    }

    [Fact]
    public void A_missing_directory_is_refused()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var act = () => SeedMigrationFile.In(missing);

        act.Should().Throw<DirectoryNotFoundException>();
    }
}
