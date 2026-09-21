using System.Buffers.Binary;

namespace CoreRentalNet.CatalogIngestion.Storage;

/// <summary>Turns a vector into the bytes stored in the file.</summary>
/// <remarks>
/// <para>
/// Little-endian explicitly rather than through <c>MemoryMarshal</c> or <c>BitConverter</c>, both of which
/// write the running machine's byte order. The file can be copied between machines, and a vector that read
/// back as a different set of floats would rank the catalogue wrongly rather than fail — the worst shape a
/// defect can have here.
/// </para>
/// <para>
/// <b>There is no decoder here, because the tool never reads.</b> The table is written whole and read by
/// whatever consumes it, so a reading half in this project would be code no run can reach. A consumer decodes
/// the format it is told to expect: <b>384 little-endian IEEE-754 floats, 1,536 bytes</b>.
/// </para>
/// </remarks>
internal static class VectorBlob
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
}
