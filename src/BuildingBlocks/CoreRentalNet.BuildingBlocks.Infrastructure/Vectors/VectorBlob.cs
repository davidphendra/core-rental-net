using System.Buffers.Binary;

namespace CoreRentalNet.BuildingBlocks.Infrastructure.Vectors;

/// <summary>
/// Turns a vector into the bytes stored in a file, and back.
/// </summary>
/// <remarks>
/// <para>
/// Little-endian explicitly rather than through <c>MemoryMarshal</c> or <c>BitConverter</c>, both of which
/// write the running machine's byte order. A vector file can be copied between machines, and a vector that
/// silently read back as a different set of floats would rank the catalogue wrongly rather than fail — the
/// worst shape a defect can have here.
/// </para>
/// <para>
/// <b>Both directions are here because two projects share one format.</b> The ingestion tool writes vectors
/// and never reads them; the application reads them and never writes. One type holding the one statement of
/// the byte order is what stops the writer and the reader from disagreeing about it — a disagreement that
/// would otherwise stay invisible until the two met, which is exactly when it does damage.
/// </para>
/// </remarks>
public static class VectorBlob
{
    /// <summary>The bytes of a vector, as stored.</summary>
    public static byte[] ToBytes(float[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var bytes = new byte[values.Length * sizeof(float)];

        for (var index = 0; index < values.Length; index++)
        {
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(index * sizeof(float)), values[index]);
        }

        return bytes;
    }

    /// <summary>The vector a stored row holds.</summary>
    public static float[] ToFloats(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        if (bytes.Length % sizeof(float) != 0)
        {
            throw new ArgumentException(
                $"A stored vector must be a whole number of {sizeof(float)}-byte floats, but was {bytes.Length} bytes.",
                nameof(bytes));
        }

        var values = new float[bytes.Length / sizeof(float)];

        for (var index = 0; index < values.Length; index++)
        {
            values[index] = BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(index * sizeof(float)));
        }

        return values;
    }
}
