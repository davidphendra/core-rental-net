using System.Security.Cryptography;

namespace CoreRentalNet.BuildingBlocks.Infrastructure.Hashing;

/// <summary>
/// The identity of a catalogue file, as the bytes it was read from.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two sides compute it and one compares them</b>: the ingestion tool records the hash of the file it built
/// vectors from, and the application hashes the file it would build from today and refuses an index whose two
/// hashes disagree. A second implementation of that hash is a comparison that cannot be trusted, so there is
/// one.
/// </para>
/// <para>
/// It is not the suggestion run's payload hash. Those are two different facts: the payload hash describes the
/// projection a model was shown, and this describes the file a set of vectors was built from.
/// </para>
/// <para>
/// Hex rather than base64 so a value is safe in a log line, a command line and a SQL literal, and lowercase so
/// two runs of the same file cannot differ by case alone.
/// </para>
/// </remarks>
public static class CatalogHash
{
    /// <summary>The hash of a catalogue file's bytes.</summary>
    public static string OfFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var file = File.OpenRead(path);

        return Of(file);
    }

    /// <summary>The hash of a catalogue's bytes, whatever they came from.</summary>
    public static string Of(Stream catalogue)
    {
        ArgumentNullException.ThrowIfNull(catalogue);

        var digest = SHA256.HashData(catalogue);

        return Convert.ToHexString(digest).ToLowerInvariant();
    }
}
