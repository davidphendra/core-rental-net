namespace CoreRentalNet.CatalogIngestion;

/// <summary>What an operator sees with <c>--help</c> or after an unusable configuration.</summary>
/// <remarks>
/// It is a type of its own rather than a constant on <see cref="IngestionSettings"/>, because it is what the
/// tool says about itself and not part of what a run is configured with. The two change for different
/// reasons: a new key changes both, a changed default changes neither.
/// </remarks>
internal static class Usage
{
    public const string Text = """
        Builds the catalogue's vector index from catalog.db, or lifts an already-built vector file into the
        Discovery module's migration.

        (no arguments)  read catalog.db, embed every product and write the vector file
        --seed          read the vector file and write the *_SeedProductEmbeddings.cs migration

        Every value has a default; a key is only needed when it differs. A value that is set but
        unusable stops the run rather than falling back. The endpoint has no default; seed mode does
        not read it, because a file that is already built has nothing left to embed.

        Embedding:Provider       AzureOpenAI or OpenAiCompatible                (default AzureOpenAI)
        Embedding:Endpoint       the embedding server's OpenAI-compatible URL   (required to embed)
        Embedding:ApiKey         the server's key; a local server may omit it   (required for AzureOpenAI)
        Embedding:Model          the model the server serves                    (default text-embedding-3-small)
        Embedding:Width          how many floats each vector holds              (default 384)
        Catalog:DatabasePath     path to catalog.db                         (default ../../Host/CoreRentalNet.Host/App_Data/catalog.db)
        Database:Path            the vector SQLite file written, or read by --seed (default the Host's App_Data/discovery.db)
        Seed:MigrationsDirectory the folder holding the *_SeedProductEmbeddings.cs shell
                                                                              (default the Discovery module's Migrations)

        Chunker:TokenLimit       the most tokens one chunk may hold           (default 256)
        Chunker:BufferSize       sentences kept each side of a breakpoint     (default 1)
        Chunker:ThresholdType    Percentile, StandardDeviation, InterQuartile or Gradient (default Percentile)
        Chunker:ThresholdAmount  the breakpoint amount for the chosen type    (default 95)
        Chunker:TargetChunkCount exact chunk count, overriding the thresholds (default unset)
        Chunker:MinChunkChars    chunks shorter than this are dropped         (default 1)
        Chunker:MaxOverrunChars  how far past the limit to look for a newline (default 200)

        Values are read from appsettings.json and appsettings.Local.json beside this program.
        """;
}
