using DotNetEnv;

namespace CoreRentalNet.Agents.Hosting;

/// <summary>Fills the process environment from a local .env file, for a developer's machine.</summary>
/// <remarks>
/// A hosted container ships no .env file, so <see cref="Apply"/> is a no-op there; it serves the developer's own
/// run. It lives here rather than inline in the composition root because it mutates process state, which is the
/// one thing a composition root should not do by hand.
/// </remarks>
internal static class LocalEnvironmentDefaults
{
    /// <summary>Loads .env without clobbering anything set, then refills variables that are blank.</summary>
    public static void Apply()
        => Fill(Env.NoClobber().TraversePath().Load());

    /// <summary>The refill rule, without the file or the process.</summary>
    /// <remarks>
    /// A platform that maps a setting from an unset azd variable injects it empty, and NoClobber keeps the empty
    /// value, so an empty value is refilled from the same file — a local default stands in for a variable nobody
    /// set, while anything the platform actually set is left alone.
    /// </remarks>
    internal static void Fill(IEnumerable<KeyValuePair<string, string>> defaults)
        => Fill(defaults, Environment.GetEnvironmentVariable, Environment.SetEnvironmentVariable);

    internal static void Fill(
        IEnumerable<KeyValuePair<string, string>> defaults,
        Func<string, string?> read,
        Action<string, string?> write)
    {
        ArgumentNullException.ThrowIfNull(defaults);
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(write);

        foreach (var (key, value) in defaults)
        {
            if (string.IsNullOrWhiteSpace(read(key)))
            {
                write(key, value);
            }
        }
    }
}
