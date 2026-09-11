namespace CoreRentalNet.Host.Composition;

/// <summary>A developer's own settings, which must never reach another environment.</summary>
internal static class LocalSettings
{
    public static void AddLocalDevelopmentSettings(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Development only, and deliberately so: a local override file must never be able to
        // influence a deployed environment, where a stray copy in a published directory would
        // otherwise override production settings without anyone noticing.
        //
        // Every other environment takes its configuration from appsettings.json,
        // appsettings.{Environment}.json and the environment itself. Development adds
        // appsettings.Development.json (the tracked, empty template) and then this file, which is
        // gitignored and wins because it is loaded last.
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
        }
    }
}
