using Microsoft.Extensions.AI;
using SemanticChunkerNET;

namespace CoreRentalNet.CatalogIngestion.Chunking;

/// <summary>The semantic splitter, configured from this tool's settings.</summary>
/// <remarks>
/// <para>
/// <b>The text is cut where the meaning shifts</b>, not at a fixed length: the library embeds each sentence
/// window, measures the distance between neighbours, and breaks where that distance passes the configured
/// threshold. A product's own description therefore decides its chunks, so a long passage about one material
/// does not share a vector with the unrelated lines that follow it.
/// </para>
/// <para>
/// <b>The library's version is not recorded anywhere.</b> Nothing in this tool stores what built a vector, so
/// an upgrade that moved the boundaries would be silent — the configuration file is the only statement of the
/// chunking, and it says nothing about the code that performed it.
/// </para>
/// </remarks>
internal sealed class SemanticProductChunker : IProductChunker
{
    private readonly SemanticChunker _chunker;

    /// <summary>The splitter for one embedding generator and one set of settings.</summary>
    public SemanticProductChunker(IEmbeddingGenerator<string, Embedding<float>> generator, ChunkerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(settings);

        _chunker = new SemanticChunker(
            generator,
            settings.TokenLimit,
            settings.BufferSize,
            Type(settings.ThresholdType),
            settings.ThresholdAmount,
            settings.TargetChunkCount,
            settings.MinChunkChars,
            settings.MaxOverrunChars);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<float[]>> ChunkAsync(string text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);

        var chunks = await _chunker.CreateChunksAsync(text, cancellationToken).ConfigureAwait(false);

        return [.. chunks.Select(chunk => chunk.Embedding.Vector.ToArray())];
    }

    /// <summary>The breakpoint strategy, named the way the library names it.</summary>
    private static BreakpointThresholdType Type(string stated)
        => Enum.TryParse<BreakpointThresholdType>(stated, ignoreCase: true, out var parsed)
            ? parsed
            : throw new ArgumentException($"'{stated}' is not a chunking breakpoint type.", nameof(stated));
}
