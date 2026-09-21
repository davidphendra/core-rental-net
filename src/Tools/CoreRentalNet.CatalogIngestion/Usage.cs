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
        Builds the catalogue's vector index from products.json.

        Every value has a default; a key is only needed when it differs. A value that is set but
        unusable stops the run rather than falling back.

          Llm:Server               the OpenAI-compatible embedding server       (default http://localhost:8080/v1)
          Llm:Model                the embedding model it serves                (default all-MiniLM-L6-v2-embedding)
          Embedding:Width          how many floats each vector holds            (default 384)
          Catalog:FilePath         path to products.json                        (default ../../shared/data/products.json)
          Database:Path            the SQLite file written                      (default App_Data/product_embedding.db)

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
