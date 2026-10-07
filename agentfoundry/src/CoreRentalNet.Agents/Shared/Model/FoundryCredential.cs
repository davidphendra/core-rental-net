using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Hosting;

namespace CoreRentalNet.Agents.Shared.Model;

/// <summary>Chooses the identity this process presents to Foundry, for where the process is running.</summary>
/// <remarks>
/// Two branches, deliberately not a chain. A hosted container has a system-assigned managed identity that
/// answers in milliseconds, so it is named outright. A developer's machine has no managed identity at all,
/// and a chain that reaches it spends about a hundred seconds failing — which is also the model client's own
/// network timeout, so the first token request is cancelled, retried and cancelled again as "Retry failed
/// after 4 tries". Naming the developer's own <c>az</c> sign-in skips every source that cannot answer.
/// </remarks>
internal static class FoundryCredential
{
    public static TokenCredential Create(IHostEnvironment environment)
        => environment.IsDevelopment()
            ? new AzureCliCredential()
            : new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
}
