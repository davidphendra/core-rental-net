namespace CoreRentalNet.Host.Composition;

/// <summary>What the Blazor host itself needs, independent of any module.</summary>
internal static class PresentationRegistration
{
    public static void AddPresentation(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddHttpContextAccessor();

        // Registered rather than read from the static clock, so the scheduler and the business
        // calendar can be driven by a test.
        builder.Services.AddSingleton(TimeProvider.System);
    }
}
