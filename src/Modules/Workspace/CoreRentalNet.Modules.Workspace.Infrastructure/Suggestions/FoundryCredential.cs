using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Hosting;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>Chooses the identity the application presents to Foundry, for where the application is running.</summary>
/// <remarks>
/// Two branches, deliberately not a chain. A hosted App Service has a system-assigned managed identity that
/// answers in milliseconds, so it is named outright. A developer's machine has no managed identity at all, and a
/// chain that reaches it spends about a hundred seconds failing - which is also the model client's own network
/// timeout, so the first token request is cancelled, retried and cancelled again as "Retry failed after 4 tries".
/// Naming the developer's own <c>az</c> sign-in skips every source that cannot answer.
/// <para>
/// The environment name, not the hosting variable, is what every deployment already sets and what a developer
/// sets locally, so the one setting decides the branch everywhere. The agent's own host makes the same choice
/// the same way.
/// </para>
/// </remarks>
internal static class FoundryCredential
{
    public static TokenCredential Create(IHostEnvironment environment)
        => environment.IsDevelopment()
            ? new AzureCliCredential()
            : new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
}
