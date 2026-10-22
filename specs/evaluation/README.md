# Workspace suggestion evaluation

The harness treats every row of `Test_Script.md` as its own test case: **200 cases**, each run three times.
The 59 unique sentence/setup combinations are a diagnostic rollup, not the suite size.

## Commands

```bash
# Check the 200-row fixture against the catalogue before spending a model call.
dotnet run --project evaluation/src/CoreRentalNet.Evaluation -- validate-cases

# Run the live evaluation against a running agent host and application.
# The default local auth mode acquires a token with offline_access and refreshes it
# in memory when it is within 30 seconds of expiry.
dotnet run --project evaluation/src/CoreRentalNet.Evaluation -- run \
  --auth-mode local \
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

Authentication preflight runs before any case: it checks the token, calls MCP `initialize`/`tools/list`,
and requires both catalogue tools. A case has a **180-second budget**. The first timeout stops the whole
run, writes a partial report, and exits with code `3`.

| Exit code | Meaning |
|---:|---|
| `0` | Every selected case passed |
| `1` | Run completed with one or more failing cases |
| `2` | Authentication or preflight failed |
| `3` | Run stopped because a case timed out |
| `4` | Invalid arguments or fixture |

For a single static token, use `--auth-mode static --caller-token <token>`. The static token is a
diagnostic fallback; it is not refreshed.

## Metrics

Primary gates: exact SKU answer correctness, constraint compliance, selected retrieval recall,
tool-call accuracy, workflow correctness, and regression rate. The detailed report also carries
diagnostics such as answer F1, pool recall, component recall, MRR, stability and Foundry judge scores.

## Local model run

The agent can use a local OpenAI-compatible server instead of Foundry. The Host can use the same kind of
server for query embeddings.

```bash
# Agent host — local chat model
MODEL_PROVIDER=OpenAiCompatible \
OPENAI_COMPATIBLE_ENDPOINT=http://localhost:8080/v1 \
OPENAI_COMPATIBLE_API_KEY=local \
AZURE_AI_MODEL_DEPLOYMENT_NAME=Qwen3-Coder-30B-A3B-Instruct \
CATALOGTOOLS__MCPENDPOINT=http://localhost:5199/mcp \
EVALUATION_TRACE_PATH=artifacts/evaluation/traces \
dotnet run --project agentfoundry/src/CoreRentalNet.Agents

# Catalogue vectors — local embedding model
dotnet run --project src/Tools/CoreRentalNet.CatalogIngestion
```

The Host reads its local embedding settings from the gitignored `appsettings.Local.json`:

```json
"VectorEmbedding": {
  "Provider": "OpenAiCompatible",
  "Endpoint": "http://localhost:8080/v1",
  "Model": "all-MiniLM-L6-v2-embedding",
  "Width": 384
}
```

The local agent path still requires the model and server to emit OpenAI `tool_calls` for
`search_catalogue` and `search_similarity_catalogue`. A chat model that only answers in prose cannot run
the retrieval stages.

The Foundry judge is optional and cloud-only. Leave `FOUNDRY_PROJECT_ENDPOINT` unset when running the
evaluation locally to keep the deterministic checks and avoid a judge call.

## Scopes

`run` defaults to `--scope tiny`. The scopes select evenly spaced rows, preserving script order.

| Scope | Rows |
|---|---:|
| `tiny` | 5 |
| `small` | 25 |
| `half` | half, rounded up |
| `all` | all 200 |

```bash
# Cheap smoke run: 5 cases, one repetition, 180 seconds per case
dotnet run --project evaluation/src/CoreRentalNet.Evaluation -- run \
  --auth-mode local --scope tiny --repetitions 1 ...

# Full baseline
dotnet run --project evaluation/src/CoreRentalNet.Evaluation -- run --scope all ...
```

Fixture validation always checks all rows, regardless of scope. Reports record both the selected
scope and the full fixture size. A baseline and a current report with different scopes are not
comparable.

## Timing report

When `EVALUATION_TRACE_PATH` is set, the agent also writes one span file per run under `spans/`. The report
combines those spans with the Host's `Server-Timing` response header.

The report separates:

- workflow-node total;
- stage model-call time;
- tool/MCP transport time;
- guardrail time;
- Host total, embedding and vector-search time;
- unattributed executor/workflow time.

A model-backed node whose model span did not stop before a timeout is attributed to model time; the report
marks the case with `CASE_TIMEOUT` and keeps the measured node duration.
