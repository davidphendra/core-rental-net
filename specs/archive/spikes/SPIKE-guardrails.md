# SPIKE — guardrail middleware seam

Phase 0 of the guardrail plan. It answers one question: **is MAF function middleware the right seam for
tool-call guardrails in this deployable?**

## What was verified from the pinned packages (not assumed)

Read from `Microsoft.Agents.AI` **1.20.0** — the version this repo pins, not the 1.8/1.23 docs the design
cited:

| Fact | Evidence |
|---|---|
| Function middleware exists | `Microsoft.Agents.AI.FunctionInvocationDelegatingAgentBuilderExtensions.Use(this AIAgentBuilder, Func<AIAgent, FunctionInvocationContext, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>>, CancellationToken, ValueTask<object?>>)` |
| The context exposes what a guardrail needs | `FunctionInvocationContext.{Function, Arguments, CallContent, Messages, Options, Iteration, FunctionCallIndex, FunctionCount, Terminate, IsStreaming}` |
| Blocking is supported | `FunctionInvocationContext.Terminate` |
| It is the framework's own pattern | `UseToolApproval(ToolApprovalAgentOptions)`, `UseLogging`, `UseOpenTelemetry` are siblings on the same builder |
| The delegate is nullable-valued | the `Func`'s result is `object?`, which is why the guardrail contracts carry `object?` |

The design's premise holds: MAF offers a function-invocation seam, and it is the only first-class interception
point. The other seams (`DelegatingChatClient`, an `AIFunction` decorator) already existed and are what the
old token-leak guardrail and the old recorder used.

## What is **not** yet verified by execution

These need the agent run against a fake model, and are the reason this is a spike rather than a decision:

1. **Does `Use(...)` fire for a tool attached via `ChatOptions.Tools`?** `AuthorisedMcpChatClient` attaches the
   MCP tools on `ChatOptions`, not as functions registered on the agent. If MAF's middleware only intercepts
   agent-registered functions, the seam is the wrong one and the adapter must move to a tool wrapper.
2. **Ordering with `UseOpenTelemetry`.** `AgentFactory.Build` calls `.UseOpenTelemetry(...)` then
   `.Use(guardrailMiddleware.InvokeAsync)`. Whether that order is the execution order is unverified, and MAF's
   function-middleware composition has had ordering changes between releases.
3. **Coverage for `RunAsync<T>`.** The workspace stages use `RunAsync<T>` (non-streaming); whether the
   middleware runs there, and for streaming, is unverified.
4. **Double interception.** The recorder has moved into the result pipeline, so there is one interception layer
   now — but whether MAF's own function layer and `FunctionInvokingChatClient` both surface a call to the
   middleware is unverified.

## What the code does today

The middleware is attached in `AgentFactory.Build` (one place, stage agents only, never the served workflow
agent). It runs the pipeline the registration assembled, in registration order. If item 1 above proves false,
only `AgentFactory.Build` and the adapter move; the policies, the pipeline and the tests do not.

## Open item

Write the execution harness (a fake `IChatClient` returning a tool call, a tool on `ChatOptions.Tools`, a
middleware that records that it ran) and record the four answers. Until then this seam is *observed*, not
*proven* — the same standard the observability spike holds itself to.
