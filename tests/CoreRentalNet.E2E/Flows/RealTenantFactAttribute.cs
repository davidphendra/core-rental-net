using Xunit;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// A <c>[Fact]</c> that skips itself unless the tenant credentials are configured, so the suite stays
/// green and offline by default.
/// </summary>
internal sealed class RealTenantFactAttribute : FactAttribute
{
    public RealTenantFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CORERENTAL_TENANT_EMAIL"))
            || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CORERENTAL_TENANT_PASSWORD")))
        {
            Skip = "Set CORERENTAL_TENANT_EMAIL and CORERENTAL_TENANT_PASSWORD to run against the real tenant. " +
                   "Add CORERENTAL_TENANT_AUDIENCE when the tenant has an API for the gate's permission.";
        }
    }
}
