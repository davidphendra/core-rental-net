using CoreRentalNet.Evaluation.Cases;

namespace CoreRentalNet.Evaluation.Tests;

/// <summary>The single parsed fixture used by the manifest and live evaluation tests.</summary>
public static class TestScriptManifest
{
    private static readonly LoadedCaseSet Loaded = CaseSetLoader.Load(
        Path.Combine(AppContext.BaseDirectory, "Test_Script.md"),
        Path.Combine(AppContext.BaseDirectory, "products.json"));

    public static IReadOnlyList<EvaluationCase> Cases => Loaded.Cases.Cases;

    public static LoadedCaseSet LoadedSet => Loaded;
}
