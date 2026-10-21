using System.ClientModel;
using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using CoreRentalNet.BuildingBlocks.Infrastructure.Embeddings;
using CoreRentalNet.BuildingBlocks.Infrastructure.Hashing;
using CoreRentalNet.BuildingBlocks.Infrastructure.Vectors;
using CoreRentalNet.CatalogIngestion;
using CoreRentalNet.CatalogIngestion.Chunking;
using CoreRentalNet.CatalogIngestion.Embeddings;
using CoreRentalNet.CatalogIngestion.Reporting;
using CoreRentalNet.CatalogIngestion.Seeding;
using CoreRentalNet.CatalogIngestion.Storage;
using CoreRentalNet.Modules.Catalog.Infrastructure.Contracts;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

// The catalogue ingestion: the one deliberate step that builds the vectors the suggester reads, and the
// seed mode that lifts vectors already built into the Discovery module's migration.
//
// It is a console host rather than a service, because it is an operator's action and not something that
// happens while customers are being served. It is configured by appsettings.json beside it rather than by a
// command line, so the only arguments it understands are --help and --seed.

if (args.Contains("--help", StringComparer.Ordinal))
{
    Console.WriteLine(Usage.Text);
    return 0;
}

var seed = args.Contains("--seed", StringComparer.Ordinal);
var unknown = args.FirstOrDefault(argument => argument is not "--seed");

if (unknown is not null)
{
    Console.Error.WriteLine(
        $"'{unknown}' is not an option. This tool is configured by appsettings.json.{Environment.NewLine}"
        + Usage.Text);
    return 2;
}

var reporter = new SpectreProgressReporter();

if (seed)
{
    AnsiConsole.MarkupLine("[bold]Seed[/]");

    try
    {
        return await reporter.RunAsync(() => Task.FromResult(Seed()));
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(Describe(exception));
        return 1;
    }
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

AnsiConsole.MarkupLine(
    $"[bold]Ingest[/] · catalogue {Markup.Escape(settings.CataloguePath)} · "
    + $"model {Markup.Escape(settings.Embedding.Model)} · database {Markup.Escape(settings.DatabasePath)}");

try
{
    return await reporter.RunAsync(() => Run());
}
catch (Exception exception)
{
    Console.Error.WriteLine(Describe(exception));
    return 1;
}

// The seed mode: no server, no key, no chunker. It lifts the vectors already in the tool's file into the
// Discovery module's migration, so the store is carried by source control rather than by a binary beside the
// application. The endpoint and the key have no part in a file that is already built.
int Seed()
{
    var configuration = Configuration();
    var baseDirectory = ProjectDirectory.Resolve();

    var source = Resolve(configuration, "Database:Path", IngestionSettings.DefaultDatabasePath, baseDirectory);
    var migrations = Resolve(
        configuration,
        "Seed:MigrationsDirectory",
        "../../Modules/Discovery/CoreRentalNet.Modules.Discovery.Infrastructure/Migrations",
        baseDirectory);

    var file = SeedMigrationFile.In(migrations);
    var seed = SqliteEmbeddingSeedReader.Read(source, reporter);

    SeedMigrationWriter.Write(file.Path, seed, reporter);

    reporter.Complete($"Wrote {seed.Vectors.Count} vectors from {source} into {file.Path}.");
    return 0;
}

// The one place every collaborator is built and handed to the run. It reads the settings from the enclosing
// scope rather than being given them again, because this is the only code that composes the tool.
async Task<int> Run()
{
    // The same loader the application reads the file with, so what is embedded is what a caller would read.
    reporter.Report(new ProgressUpdate("Reading catalogue", 0, 1, settings.CataloguePath));
    var catalogue = CatalogueFile.Read(settings.CataloguePath);
    reporter.Report(new ProgressUpdate("Reading catalogue", 1, 1, $"{catalogue.Count} products"));

    var embeddings = EmbeddingServiceFactorySelector
        .Select(settings.Embedding.Provider)
        .Create(settings.Embedding);
    var semanticProductChunker = new SemanticProductChunker(new ClientEmbeddingGenerator(embeddings), settings.Chunker);
    var store = new SqliteProductEmbeddingStore(settings.DatabasePath);

    var written = await new IngestionPipeline(
            catalogue,
            semanticProductChunker,
            store,
            reporter,
            Recipe(),
            TimeProvider.System
        )
        .RunAsync(CancellationToken.None);

    reporter.Complete($"Wrote {written} vectors from {settings.CataloguePath} into {settings.DatabasePath}.");
    return 0;
}

// What built the vectors, written into the file beside them: the model, the width, the composition of the
// text, and the hash of the catalogue file this run actually read. The application compares all four against
// its own configuration before it searches anything, so a disagreement hides the feature and names this tool
// instead of ranking vectors that are not comparable.
EmbeddingRecipe Recipe()
    => new(
        settings.Embedding.Model,
        settings.Embedding.Width,
        ProductVectorContract.Composition,
        CatalogHash.OfFile(settings.CataloguePath));

// A seed mode path: as stated, or the fallback, resolved against the tool's project directory so a relative
// value means the same thing however the program was launched.
static string Resolve(IConfiguration configuration, string key, string fallback, string baseDirectory)
{
    var stated = configuration[key]?.Trim();
    var value = string.IsNullOrEmpty(stated) ? fallback : stated;

    return Path.IsPathRooted(value) ? value : Path.Combine(baseDirectory, value);
}

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
