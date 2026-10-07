using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Shared.Scoping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The served graph is one instance, so what a run owns has to come from the request and not the root.</summary>
/// <remarks>
/// This is the property the whole run-scope indirection exists for. Hosting resolves the served agent once from
/// the root container, so a graph-level client that reached the root would hand every concurrent run the first
/// run's token service and ledger. The graph-level accessor is built once here, exactly as the graph builds it,
/// and must return each request's own instance.
/// </remarks>
public sealed class RunScopeTests
{
    [Fact]
    public void Each_request_reaches_its_own_run_scoped_services()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddScoped<IMcpAccessTokenService, McpAccessTokenService>();
        services.AddScoped<McpToolAnswerLedger>();
        services.AddSingleton<IRunScope, RunScope>();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });

        var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();

        // Built once, from the root, exactly as the served graph is.
        var runScope = provider.GetRequiredService<IRunScope>();

        using var firstRequest = provider.CreateScope();
        httpContextAccessor.HttpContext = new DefaultHttpContext { RequestServices = firstRequest.ServiceProvider };

        var firstTokens = runScope.Resolve<IMcpAccessTokenService>();
        var firstLedger = runScope.Resolve<McpToolAnswerLedger>();
        firstTokens.Token = "caller-a";

        using var secondRequest = provider.CreateScope();
        httpContextAccessor.HttpContext = new DefaultHttpContext { RequestServices = secondRequest.ServiceProvider };

        var secondTokens = runScope.Resolve<IMcpAccessTokenService>();
        var secondLedger = runScope.Resolve<McpToolAnswerLedger>();

        firstTokens.Should().NotBeSameAs(
            secondTokens, "one instance's graph must not present the first caller's token to the second run");
        firstLedger.Should().NotBeSameAs(
            secondLedger, "one attempt's tool answers must not accumulate in another run's ledger");
        secondTokens.Token.Should().BeNull("the second request carried no token of its own");
    }
}
