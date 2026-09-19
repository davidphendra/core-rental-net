using System.Net;
using System.Net.Sockets;

namespace CoreRentalNet.E2E;

/// <summary>
/// Where the assemblies the suite starts live, and where a spare port can be had.
/// </summary>
/// <remarks>
/// Resolved from the test assembly's own location rather than assumed, and in the configuration the
/// tests were built in, because that is the configuration the applications beside them were built in.
/// </remarks>
internal static class TestPaths
{
    /// <summary>The application's assembly, which the suite starts.</summary>
    public static string HostAssembly()
        => Assembly("src", "Host", "CoreRentalNet.Host", "CoreRentalNet.Host.dll");

    /// <summary>The local provider's assembly, which the suite starts and the guard test probes.</summary>
    public static string LocalProviderAssembly()
        => Assembly("tests", "CoreRentalNet.E2E.LocalProvider", "CoreRentalNet.E2E.LocalProvider.dll");

    /// <summary>
    /// The application's own directory, so a development build finds its static assets exactly as
    /// <c>dotnet run</c> would.
    /// </summary>
    public static string ProjectDirectory()
        => Path.Combine(RepositoryRoot(), "src", "Host", "CoreRentalNet.Host");

    public static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CoreRentalNet.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }

    /// <summary>A port nothing is listening on, so two hosts in one run cannot collide.</summary>
    public static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        return port;
    }

    private static string Assembly(params string[] parts)
    {
        var configuration = AppContext.BaseDirectory.Contains(
            $"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}",
            StringComparison.Ordinal)
            ? "Release"
            : "Debug";

        var path = Path.Combine([RepositoryRoot(), .. parts[..^1], "bin", configuration, "net10.0", parts[^1]]);

        return File.Exists(path)
            ? path
            : throw new FileNotFoundException($"Not built: {path}");
    }
}
