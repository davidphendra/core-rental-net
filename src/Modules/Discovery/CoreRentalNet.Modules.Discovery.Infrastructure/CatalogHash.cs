using System.Security.Cryptography;
using System.Text;

namespace CoreRentalNet.Modules.Discovery.Infrastructure;

/// <summary>
/// The identity of the catalogue a vector was built from.
/// </summary>
/// <remarks>
/// <para>
/// It is a hash of the file <b>as it was read</b>, and it is not the suggestion run's payload hash. Those are
/// two different facts: the payload hash describes the projection a model was shown, and this describes the
/// file an index was built from. ADR 0002 already had to say why the first is not a hash of the file; this is
/// the other half of the same distinction.
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
