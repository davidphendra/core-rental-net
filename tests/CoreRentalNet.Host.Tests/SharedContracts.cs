using Json.Schema;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The contract schemas, parsed once for the whole test assembly.
/// </summary>
/// <remarks>
/// <para>
/// <b>Once, and that is a constraint rather than tidiness.</b> <c>JsonSchema.FromText</c> registers each schema
/// globally by its <c>$id</c>, and registering the same one twice throws. Two test classes each parsing these
/// files therefore fail whichever of them runs second - which is exactly what happened when e06 added a second
/// contract test, and it failed in whichever order the runner chose.
/// </para>
/// <para>
/// The two trees share no project reference, so these files are the only thing that binds the application's
/// DTOs to the agent's contract; every class that checks them reads from here rather than parsing its own copy.
/// </para>
/// </remarks>
internal static class SharedContracts
{
    private const string Directory = "contracts";

    /// <summary>Every schema under <c>agentfoundry/shared/contracts</c>, keyed by file name.</summary>
    public static IReadOnlyDictionary<string, JsonSchema> Schemas { get; } =
        System.IO.Directory
            .GetFiles(Path.Combine(AppContext.BaseDirectory, Directory), "*.json")
            .ToDictionary(
                file => Path.GetFileName(file)!,
                file => JsonSchema.FromText(File.ReadAllText(file))!);
}
