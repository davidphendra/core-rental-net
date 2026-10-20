using CoreRentalNet.Evaluation.Commands;

return args switch
{
    ["validate-cases", .. var rest] => await ValidateCasesCommand.RunAsync(rest),
    ["run", .. var rest] => await RunEvaluationCommand.RunAsync(rest),
    ["compare", .. var rest] => await CompareBaselineCommand.RunAsync(rest),
    [] => PrintUsage(),
    _ => PrintUsage(),
};

static int PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  validate-cases [Test_Script.md] [products.json]");
    Console.WriteLine("  run --agent-endpoint <uri> --caller-token <token> [--mcp-endpoint <uri>]");
    Console.WriteLine("  compare <baseline.json> <current.json>");
    return 1;
}
