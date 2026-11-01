using AwesomeAssertions;
using CoreRentalNet.Evaluation.Commands;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Runs;

/// <summary>The command refuses the lockbox before it reaches the network, and writes nothing.</summary>
public sealed class LockboxContainmentTests
{
    [Fact]
    public async Task Running_the_test_dataset_without_confirmation_refuses_and_stores_nothing()
    {
        var reports = Path.Combine(Path.GetTempPath(), $"runs-{Guid.NewGuid():N}");

        var exit = await RunDatasetCommand.RunAsync(["--dataset", "test", "--limit", "1", "--reports-dir", reports]);

        exit.Should().Be(2);
        Directory.Exists(reports).Should().BeFalse();
    }
}
