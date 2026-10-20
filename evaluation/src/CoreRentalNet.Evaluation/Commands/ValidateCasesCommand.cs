using CoreRentalNet.Evaluation.Cases;

namespace CoreRentalNet.Evaluation.Commands;

/// <summary>Checks the fixture before a model call is spent.</summary>
public static class ValidateCasesCommand
{
    public static Task<int> RunAsync(string[] args)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var testScript = args.ElementAtOrDefault(0) ?? Path.Combine(baseDirectory, "Test_Script.md");
        var products = args.ElementAtOrDefault(1) ?? Path.Combine(baseDirectory, "products.json");

        var loaded = CaseSetLoader.Load(testScript, products);

        Console.WriteLine($"Rows:                 {loaded.Cases.Cases.Count}");
        Console.WriteLine($"Unique sentences:     {loaded.Cases.UniqueSentenceCount}");
        Console.WriteLine($"Unique setups:        {loaded.Cases.UniqueSetupCount}");
        Console.WriteLine($"Catalogue products:   {loaded.Catalogue.Count}");
        Console.WriteLine($"Validation:           {(loaded.Validation.IsValid ? "PASS" : "FAIL")}");

        foreach (var warning in loaded.Validation.Warnings)
        {
            Console.WriteLine($"WARNING: {warning}");
        }

        foreach (var error in loaded.Validation.Errors)
        {
            Console.WriteLine($"ERROR: {error}");
        }

        return Task.FromResult(loaded.Validation.IsValid ? 0 : 1);
    }
}
