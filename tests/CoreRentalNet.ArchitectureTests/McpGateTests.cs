using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

/// <summary>
/// Every MCP tool carries a gate. A tool is a capability a machine caller reaches, so a tool published without
/// an authorization policy is the same defect as a controller action without one - and the REST surface
/// already has a test that refuses that.
/// </summary>
/// <remarks>
/// Source-level, like the other layout rules: weak but deterministic. It reads the attribute beside each
/// <c>[McpServerTool]</c> rather than the compiled metadata, which the running server is what enforces.
/// </remarks>
public sealed class McpGateTests
{
    [Fact] // MCP-03
    public void Every_mcp_tool_declares_the_policy_that_gates_it()
    {
        var tools = HostSources()
            .Where(file => File.ReadAllText(file).Contains("[McpServerTool(", StringComparison.Ordinal))
            .ToArray();

        tools.Should().NotBeEmpty("the rule must be checked against real tools");

        foreach (var file in tools)
        {
            File.ReadAllText(file).Should().Contain(
                "[Authorize(Policy = ",
                $"{Path.GetFileName(file)} publishes a tool, so it must say which permission entitles a caller");
        }
    }

    private static IEnumerable<string> HostSources()
    {
        var host = Path.Combine(RepoRoot.Path, "src", "Host");

        return Directory.GetFiles(host, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));
    }
}
