# Workspace suggestion evaluation

The harness treats every row of `Test_Script.md` as its own test case: **200 cases**, each run three times.
The 59 unique sentence/setup combinations are a diagnostic rollup, not the suite size.

## Commands

```bash
# Check the 200-row fixture against the catalogue before spending a model call.
dotnet run --project evaluation/src/CoreRentalNet.Evaluation -- validate-cases

# Run the live evaluation against a running agent host and application.
EVALUATION_CALLER_TOKEN=... \
dotnet run --project evaluation/src/CoreRentalNet.Evaluation -- run \
  --agent-endpoint http://127.0.0.1:8088 \
  --mcp-endpoint http://127.0.0.1:5199/mcp \
  --trace-directory artifacts/evaluation/traces \
  --report specs/evaluation/reports/latest.json

# Compare a current report with an accepted baseline.
dotnet run --project evaluation/src/CoreRentalNet.Evaluation -- compare \
  specs/evaluation/baselines/YYYY-MM-DD-initial.json \
  specs/evaluation/reports/latest.json
```

The agent host must be started with `EVALUATION_TRACE_PATH` pointing at the same directory passed as
`--trace-directory`. The trace is disabled when that variable is absent.

## Metrics

Primary gates: exact SKU answer correctness, constraint compliance, selected retrieval recall,
tool-call accuracy, workflow correctness, and regression rate. The detailed report also carries
diagnostics such as answer F1, pool recall, component recall, MRR, stability and Foundry judge scores.
