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
    Console.WriteLine("  run --agent-endpoint <uri> --mcp-endpoint <uri> [--auth-mode local|static]");
    Console.WriteLine("      [--caller-token <token>] [--scope tiny|small|half|all] [--repetitions <n>]");
    Console.WriteLine("      [--judge-endpoint <uri>] [--judge-mode rag|equivalence|both] [--judge-model gpt-oss-20b] [--judge-api-key local]");
    Console.WriteLine("      [--judge-timeout <seconds>] [--judge-retries <n>] [--success-rate-threshold <0..1>]");
    Console.WriteLine("      [--export json,junit,markdown,trx,csv] [--report <path>]");
    Console.WriteLine("  compare <baseline.json> <current.json>");
    return 1;
}
