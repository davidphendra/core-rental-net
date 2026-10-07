using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace CoreRentalNet.IntegrationTests;

/// <summary>The only part of the host environment the credential choice reads.</summary>
internal sealed class EnvironmentNamed(string environmentName) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = environmentName;

    public string ApplicationName { get; set; } = "CoreRentalNet.IntegrationTests";

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
