using AwesomeAssertions;
using CoreRentalNet.Agents.Hosting;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The .env refill rule. A platform that maps a setting from an unset azd variable injects it empty, so blank
/// must be refilled from the local file while a value the platform actually set is left alone.
/// </summary>
public sealed class LocalEnvironmentDefaultsTests
{
    [Fact]
    public void A_blank_variable_is_refilled_and_a_set_one_is_kept()
    {
        Dictionary<string, string?> environment = new()
        {
            ["SET"] = "kept",
            ["BLANK"] = "   ",
            ["ABSENT"] = null,
        };

        KeyValuePair<string, string>[] defaults =
        [
            new("SET", "from-file"),
            new("BLANK", "from-file"),
            new("ABSENT", "from-file"),
        ];

        LocalEnvironmentDefaults.Fill(
            defaults,
            key => environment[key],
            (key, value) => environment[key] = value);

        environment["SET"].Should().Be("kept");
        environment["BLANK"].Should().Be("from-file");
        environment["ABSENT"].Should().Be("from-file");
    }
}
