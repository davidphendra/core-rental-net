using CoreRentalNet.CatalogPopulate;
using CoreRentalNet.CatalogPopulate.Extracting;
using CoreRentalNet.CatalogPopulate.Populating;
using Microsoft.Extensions.Configuration;

// The catalogue populator: one operator action, in two modes.
//
//   --extract   scans the Hugging Face source files once and writes the compact input.
//   (default)   reads the compact input, asks the local model for the generated metadata,
//               and writes products.json plus the vendored images.
//
// It is a console host rather than a service, because it runs while customers are not being
// served, and it is configured by appsettings.json beside it rather than by a command line.

if (args.Contains("--help", StringComparer.Ordinal))
{
    Console.WriteLine(Usage.Text);
    return 0;
}

if (args.Any(arg => arg is not "--extract"))
{
    Console.Error.WriteLine($"'{args.First(arg => arg is not "--extract")}' is not an option.{Environment.NewLine}{Usage.Text}");
    return 2;
}

var mode = args.Contains("--extract", StringComparer.Ordinal) ? Mode.Extract : Mode.Populate;

CatalogPopulateSettings settings;

try
{
    settings = ConfigurationReader.Read(Configuration(), ProjectDirectory.Resolve(), mode);
}
catch (ArgumentException exception)
{
    Console.Error.WriteLine(exception.Message);
    Console.Error.WriteLine(Usage.Text);
    return 2;
}

try
{
    return mode == Mode.Extract
        ? new Extractor(settings).Run()
        : await new Populator(settings).RunAsync();
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

// appsettings.json and appsettings.Local.json beside the program. The local file is gitignored.
static IConfiguration Configuration()
    => new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile("appsettings.Local.json", optional: true)
        .Build();
