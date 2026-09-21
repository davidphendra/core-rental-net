using AwesomeAssertions;
using CoreRentalNet.CatalogIngestion.Storage;
using Xunit;

namespace CoreRentalNet.CatalogIngestion.Tests.Storage;

/// <summary>
/// The bytes a vector is stored as.
/// </summary>
/// <remarks>
/// The byte order is the point: the file can be copied between machines, and a vector that read back as a
/// different set of floats would rank the catalogue wrongly rather than fail. There is no decoder to test —
/// the tool only writes — so a consumer's expectation is pinned here as the exact bytes.
/// </remarks>
public sealed class VectorBlobTests
{
    [Fact]
    public void A_vector_is_four_bytes_per_float()
    {
        VectorBlob.ToBytes(new float[384]).Should().HaveCount(384 * sizeof(float));
    }

    [Fact]
    public void A_vector_is_written_little_endian_so_it_means_the_same_anywhere()
    {
        // 1.0f is 0x3F800000; little-endian writes the low byte first.
        VectorBlob.ToBytes([1f]).Should().Equal(0x00, 0x00, 0x80, 0x3F);
    }
}
