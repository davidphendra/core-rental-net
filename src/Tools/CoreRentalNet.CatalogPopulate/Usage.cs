namespace CoreRentalNet.CatalogPopulate;

/// <summary>What an operator sees with <c>--help</c> or after an unusable configuration.</summary>
internal static class Usage
{
    public const string Text = """
        Builds the catalogue's SeedProducts migration from the Hugging Face Amazon Reviews dataset.

        --extract   scan the source files once and write the compact input
        (default)   read the compact input, generate metadata with the local model, write the seed migration

        Llm:Endpoint             the OpenAI-compatible local server      (default http://localhost:8080/v1)
        Llm:Model                the model deployment name                (default Qwen3-Coder-30B-A3B-Instruct)
        Llm:ReviewSampleSize     reviews fed to the model per product     (default 20)

        Selection:TargetTotal    products extracted to the compact file  (default 400)

        Sources:Meta:*           the four meta_*.jsonl files              (operator-supplied)
        Sources:Reviews:*        the four review category files           (operator-supplied)

        Extract:Output              the compact file written by --extract  (default App_Data/compact-catalogue.jsonl)
        Catalog:CompactInput        the compact file read when populating   (default App_Data/compact-catalogue.jsonl)
        Catalog:MigrationsDirectory the folder holding the *SeedProducts.cs shell
        Images:VendoredPath         where product images are vendored       (default Host wwwroot/images/vendored)

        Values are read from appsettings.json and appsettings.Local.json beside this program.
        """;
}
