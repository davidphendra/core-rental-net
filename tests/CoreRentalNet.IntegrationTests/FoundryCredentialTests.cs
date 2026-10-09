using Azure.Identity;
using AwesomeAssertions;
using CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>Which identity the application presents, by environment. The branch is the whole policy.</summary>
public sealed class FoundryCredentialTests
{
    [Fact]
    public void Development_presents_the_developer_sign_in()
        => FoundryCredential.Create(new EnvironmentNamed(Environments.Development))
            .Should().BeOfType<AzureCliCredential>();

    [Fact]
    public void Anything_else_presents_the_managed_identity()
        => FoundryCredential.Create(new EnvironmentNamed(Environments.Production))
            .Should().BeOfType<DefaultAzureCredential>();
}
