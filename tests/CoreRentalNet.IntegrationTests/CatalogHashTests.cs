using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Infrastructure.Hashing;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// The identity of the catalogue an index was built from.
/// </summary>
/// <remarks>
/// It is the value that turns a stale index from a wrong answer into a fact, so the properties that matter are
/// that it is stable for unchanged bytes and different for changed ones.
/// </remarks>
public sealed class CatalogHashTests
{
    [Fact]
    public void The_same_bytes_hash_the_same_way_and_a_changed_byte_does_not()
    {
        var one = CatalogHash.Of(new MemoryStream("[{\"skuNo\":\"A\"}]"u8.ToArray()));
        var again = CatalogHash.Of(new MemoryStream("[{\"skuNo\":\"A\"}]"u8.ToArray()));
        var other = CatalogHash.Of(new MemoryStream("[{\"skuNo\":\"B\"}]"u8.ToArray()));

        one.Should().Be(again);
        other.Should().NotBe(one);
    }

    [Fact]
    public void The_hash_is_the_lowercase_hex_of_a_SHA256_digest()
    {
        var hash = CatalogHash.Of(new MemoryStream("[]"u8.ToArray()));

        // 64 characters, lowercase, hex: safe in a log line, a command line and a SQL literal.
        hash.Should().HaveLength(64).And.MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void The_hash_of_a_file_is_the_hash_of_its_bytes()
    {
        using var file = new TemporaryFile("[{\"skuNo\":\"DSKB08XN4JDR\"}]");

        var fromFile = CatalogHash.OfFile(file.Path);
        var fromMemory = CatalogHash.Of(new MemoryStream(File.ReadAllBytes(file.Path)));

        fromFile.Should().Be(fromMemory);
    }

    [Fact]
    public void A_file_that_is_not_there_is_an_error_rather_than_an_empty_hash()
    {
        // The alternative - hashing nothing - would record a constant for every unreadable catalogue and call
        // every index built from one "current".
        var act = () => CatalogHash.OfFile(Path.Combine(Path.GetTempPath(), $"absent-{Guid.NewGuid():N}.json"));

        act.Should().Throw<FileNotFoundException>();
    }
}
