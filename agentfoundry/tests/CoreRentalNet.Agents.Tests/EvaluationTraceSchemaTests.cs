using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

public sealed class EvaluationTraceSchemaTests
{
    [Fact]
    public void A_stage_model_call_satisfies_the_trace_schema()
    {
        const string Record =
            """
            {
              "kind": "stageModelCall",
              "runId": "eval-TS-001-r1",
              "sequence": 1,
              "at": "2026-10-20T12:00:00Z",
              "stage": "workspace-request-verifier",
              "attempt": 1,
              "responseText": "{\"isWorkspaceRequest\":true}",
              "toolCalls": [],
              "inputTokens": 10,
              "outputTokens": 4
            }
            """;

        Schemas.EvaluateRaw("agent-evaluation-trace.schema.json", Record).IsValid.Should().BeTrue();
    }

    [Fact]
    public void A_tool_exchange_satisfies_the_trace_schema()
    {
        const string Record =
            """
            {
              "kind": "toolExchange",
              "runId": "eval-TS-001-r1",
              "sequence": 2,
              "at": "2026-10-20T12:00:01Z",
              "stage": "catalogue-product-retriever",
              "attempt": 1,
              "callId": "call-1",
              "toolName": "search_catalogue",
              "arguments": { "category": "desk", "terms": ["desk"] },
              "resultText": "{\"matches\":{\"value\":[{\"sku\":\"D1\"}]}}"
            }
            """;

        Schemas.EvaluateRaw("agent-evaluation-trace.schema.json", Record).IsValid.Should().BeTrue();
    }

    [Fact]
    public void An_unknown_record_kind_is_refused()
    {
        const string Record =
            """
            { "kind": "somethingElse", "runId": "r", "sequence": 1, "at": "2026-10-20T12:00:00Z" }
            """;

        Schemas.EvaluateRaw("agent-evaluation-trace.schema.json", Record).IsValid.Should().BeFalse();
    }
}
