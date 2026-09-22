using System.ClientModel;
using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using CoreRentalNet.BuildingBlocks.Infrastructure.Embeddings;
using CoreRentalNet.BuildingBlocks.Infrastructure.Hashing;
using CoreRentalNet.BuildingBlocks.Infrastructure.Vectors;
using CoreRentalNet.CatalogIngestion;
using CoreRentalNet.CatalogIngestion.Chunking;
using CoreRentalNet.CatalogIngestion.Embeddings;
using CoreRentalNet.CatalogIngestion.Storage;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using CoreRentalNet.Modules.Catalog.Infrastructure.Contracts;
using Microsoft.Extensions.Configuration;

// The catalogue ingestion: the one deliberate step that builds the vector table the suggester reads.
//
// It is a console host rather than a service, because it is an operator's action and not something that
// happens while customers are being served. It is configured by appsettings.json beside it rather than by a
// command line, so the only argument it understands is --help.

if (args.Contains("--help", StringComparer.Ordinal))
{
    Console.WriteLine(Usage.Text);
    return 0;
}

if (args.Length > 0)
{
    Console.Error.WriteLine(
        $"'{args[0]}' is not an option. This tool is configured by appsettings.json.{Environment.NewLine}"
        + Usage.Text);
    return 2;
}

IngestionSettings settings;

try
{
    settings = ConfigurationReader.Read(Configuration(), ProjectDirectory.Resolve());
}
catch (ArgumentException exception)
{
    Console.Error.WriteLine(exception.Message);
    Console.Error.WriteLine(Usage.Text);
    return 2;
}

try
{
    return await Run();
}
catch (Exception exception)
{
    Console.Error.WriteLine(Describe(exception));
    return 1;
}

// The one place every collaborator is built and handed to the run. It reads the settings from the enclosing
// scope rather than being given them again, because this is the only code that composes the tool.
async Task<int> Run()
{
    // The same loader the application uses, so what is embedded is what a caller would read. The web root is
    // absent on purpose: this tool never draws an image, and passing one would only let image resolution fail
    // for a reason that does not matter here.
    var catalogue = new ProductCatalogService(settings.CataloguePath, webRootPath: null);

    var embeddings = OpenAiCompatibleEmbeddingRepository.Build(settings.LlmServer, settings.LlmModel);
    var chunker = new SemanticProductChunker(new ClientEmbeddingGenerator(embeddings), settings.Chunker);
    var store = new SqliteProductEmbeddingStore(settings.DatabasePath);

    var written = await new IngestionPipeline(catalogue, chunker, store, Recipe(), TimeProvider.System)
        .RunAsync(CancellationToken.None);

    Console.WriteLine($"Wrote {written} vectors from {settings.CataloguePath} into {settings.DatabasePath}.");
    return 0;
}

// What built the vectors, written into the file beside them: the model, the width, the composition of the
// text, and the hash of the catalogue file this run actually read. The application compares all four against
// its own configuration before it searches anything, so a disagreement hides the feature and names this tool
// instead of ranking vectors that are not comparable.
EmbeddingRecipe Recipe()
    => new(
        settings.LlmModel,
        settings.Width,
        ProductVectorContract.Composition,
        CatalogHash.OfFile(settings.CataloguePath));

// The message is what an operator can act on: it names the file, the key or the server. Anything that is not
// one of the failures this tool expects is a defect, and for a defect the stack trace is what says where it
// happened — so it is printed whole rather than reduced to its message.
static string Describe(Exception exception)
    => exception is HttpRequestException
        or IOException
        or UnauthorizedAccessException
        or ClientResultException
        or ProductLoadException
            ? exception.Message
            : exception.ToString();

// The tool's own settings file, resolved against the program rather than the working directory, with a
// gitignored Local file a developer may override it from. A missing file is not fatal: every value has a
// default, and an unusable one is refused by ConfigurationReader.
static IConfigurationRoot Configuration()
    => new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
        .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
        .Build();
