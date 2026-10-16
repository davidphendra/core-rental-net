using AwesomeAssertions;
using CoreRentalNet.CatalogPopulate.Writing;
using Xunit;

namespace CoreRentalNet.CatalogPopulate.Tests;

public sealed class SeedMigrationFileTests
{
    [Fact]
    public void The_single_shell_is_found()
    {
        using var directory = new TemporaryDirectory();
        var shell = Path.Combine(directory.Path, "20260101000000_SeedProducts.cs");
        File.WriteAllText(shell, string.Empty);

        SeedMigrationFile.In(directory.Path).Path.Should().Be(shell);
    }

    [Fact]
    public void No_shell_names_the_command_that_creates_one()
    {
        using var directory = new TemporaryDirectory();

        var act = () => SeedMigrationFile.In(directory.Path);

        act.Should().Throw<InvalidOperationException>().WithMessage("*migrations add SeedProducts*");
    }

    [Fact]
    public void A_missing_directory_is_refused()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var act = () => SeedMigrationFile.In(missing);

        act.Should().Throw<DirectoryNotFoundException>();
    }
}
