using AwesomeAssertions;
using CoreRentalNet.Agents.Features.EchoReverse;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion;
using CoreRentalNet.Agents.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// What a startup says. A container that is never acknowledged is read through this report, so the lines it
/// must carry — the served agents and the settings a deployment has to supply — are asserted rather than
/// eyeballed.
/// </summary>
public sealed class StartupReportTests
{
    private const string WorkspaceAgentName = "core-rental-workspace-suggestion-agent";

    [Fact]
    public void The_report_names_the_agent_a_nameless_request_gets()
    {
        var settings = Settings(
            (WorkspaceSuggestionAgentIdentity.ConfigurationKey, WorkspaceAgentName),
            (AgentHostSettings.DefaultAgentNameKey, EchoAgentIdentity.DefaultAgentName));

        var report = Report(settings, hosted: false);

        report.Should().Contain(AgentHostSettings.DefaultAgentNameKey);
        report.Should().Contain(EchoAgentIdentity.DefaultAgentName);
        report.Should().Contain("hosted                              : no");
    }

    [Fact]
    public void The_report_marks_a_setting_nobody_supplied_as_missing()
    {
        var report = Report(
            Settings((WorkspaceSuggestionAgentIdentity.ConfigurationKey, WorkspaceAgentName)),
            hosted: true);

        report.Should().Contain("FOUNDRY_PROJECT_ENDPOINT");
        report.Should().Contain("MISSING");
        report.Should().Contain("hosted                              : yes");
    }

    [Fact]
    public void The_report_carries_the_build_identity()
    {
        var report = Report(
            Settings((WorkspaceSuggestionAgentIdentity.ConfigurationKey, WorkspaceAgentName)),
            hosted: false);

        report.Should().Contain("version");
        report.Should().Contain("git sha");
        report.Should().Contain("build id");
        report.Should().Contain("environment");
    }

    private static string Report(AgentHostSettings settings, bool hosted)
    {
        using var writer = new StringWriter();

        StartupReport.Write(writer, settings, hosted);

        return writer.ToString();
    }

    private static AgentHostSettings Settings(params (string Key, string Value)[] values)
        => AgentHostSettings.FromConfiguration(
            new ConfigurationBuilder()
                .AddInMemoryCollection(values.ToDictionary(
                    pair => pair.Key,
                    pair => (string?)pair.Value))
                .Build());
}
