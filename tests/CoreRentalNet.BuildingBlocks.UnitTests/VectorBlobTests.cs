using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Infrastructure.Vectors;
using Xunit;

namespace CoreRentalNet.BuildingBlocks.UnitTests;

/// <summary>
/// The bytes a vector is stored as, in both directions.
/// </summary>
/// <remarks>
/// <para>
/// The byte order is the point: a vector file can be copied between machines, and a vector that read back as
/// a different set of floats would rank the catalogue wrongly rather than fail — the worst shape a defect can
/// have here.
/// </para>
/// <para>
/// The exact bytes are pinned rather than only round-tripped, because a round trip passes just as well when
/// both directions are wrong in the same way. This is the one statement of the format that does not depend on
/// either side of it being right.
/// </para>
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

    [Fact]
    public void What_the_writer_produced_is_what_the_reader_gets_back()
    {
        float[] written = [0.5f, -1.25f, 3f, 0f];

        VectorBlob.ToFloats(VectorBlob.ToBytes(written)).Should().Equal(written);
    }

    [Fact]
    public void A_blob_that_is_not_a_whole_number_of_floats_is_refused()
    {
        // Three bytes is not one float, and a reader that rounded down would return a silently short vector
        // whose every position was shifted.
        var act = () => VectorBlob.ToFloats([0x00, 0x00, 0x80]);

        act.Should().Throw<ArgumentException>().WithMessage("*whole number*");
    }
}
