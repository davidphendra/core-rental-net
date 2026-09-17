# e02s07 — The agent is deployed, and the application can call it

**type:** feat
**risk:** P0
**context:** infra
**bcps:** 5
**status:** failing

## Context

Everything so far runs locally. This story makes the agent reachable: a container image, a registry,
a released container on Foundry, an endpoint, and the identity the application uses to call it. It
also **asserts the auth path** that `e02s01` only proved by hand — including the refusal, which is the
part that matters most, because an agent that reads the catalogue without the permission would be
invisible until someone looked.

## Requirements

#### ADDED: the deployment and the identity

The container image is built from the repository root (so the central build policy files are in
context) and released to Foundry as a hosted agent. The agent's catalogue access uses whichever path
`e02s01` settled: **H1** (a Toolbox OpenAPI tool and its `oauth2` connection) or the pre-committed
fallback **A-2**, in which the container performs the Auth0 client-credentials exchange itself and
caches the token to expiry.

**No stored bearer token either way** (A-1 is refused): a credential that expires on a schedule and
fails silently at suggestion time is the failure this rule exists to prevent.

#### ADDED: configuration, not code

The Foundry project endpoint, the agent name, the catalogue endpoint and the **model deployment name**
are configuration. The model name is configuration because the `gpt-4.1-mini` family is deprecated
**no earlier than 2027-04-14**; a migration must be a configuration change, not four code changes.

## Zoom-Out

- **Module purpose:** make the workflow reachable and observable, without changing what it does.
- **Callers:** the application, over the agent endpoint (`e04s02`).
- **Contracts to preserve:** the contract schemas; the catalogue endpoint's `read:catalog` gate and its
  refusal; the endpoint's structured call log, which must record the agent's read like any other.

## Steps

1. Add the container definition and build it with the repository root as context. → verify: `docker build -f agentfoundry/src/workspace-suggestions/Dockerfile -t agentfoundry-workspace-suggestions:local .`
2. Push the image to the registry the Foundry project uses. → verify: `docker push <registry>/agentfoundry-workspace-suggestions:<version>`
3. Deploy the hosted agent with its configuration and, on the H1 path, attach the Toolbox; on the A-2
   path, configure the Auth0 client credentials. → verify: `azd deploy workspace-suggestions`
4. Attach the content-safety guardrail to the deployed agent. → verify: `azd ai agent show agentfoundry-workspace-suggestions`
5. Live test AGT-20: three schema-valid candidates for a request. → verify: `AGENTFOUNDRY_AGENT_ENDPOINT="$AGENT_ENDPOINT" dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo --filter "FullyQualifiedName~AgentLiveTests"`
6. Live tests AGT-21 … AGT-23: the refusal without `read:catalog`, the endpoint's log line, and no
   invented products for an unsatisfiable request. → verify: `AGENTFOUNDRY_AGENT_ENDPOINT="$AGENT_ENDPOINT" AGENTFOUNDRY_PROJECT_ENDPOINT="$PROJECT_ENDPOINT" dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo --filter "FullyQualifiedName~AgentLiveRiskTests"`
7. Record the image tag, agent version and instructions revision in
   `specs/verifications/e02s07-after.md`. → verify: `test -f specs/verifications/e02s07-after.md`

## Verification Script (Step-by-Step)

1. Confirm the hosted agent appears in the Foundry project with its configuration, its guardrail and —
   on the H1 path — its Toolbox.
2. Invoke it with *"a quiet corner for two monitors and a decent chair"* → three candidates, each
   naming SKUs present in the catalogue.
3. Read the application's log for exactly one catalogue read line for that run.
4. Repeat with a client lacking `read:catalog` → `403` at the endpoint and no products in the answer.
5. Send an unsatisfiable request → no invented products; either fewer candidates or a stated
   exhaustion.
6. Confirm the container's environment carries no long-lived bearer token.

## Test matrix

| ID | Scenario | Level |
|----|----------|-------|
| AGT-20 | Three schema-valid candidates for a request | live |
| AGT-21 | Without `read:catalog` the read is refused and no products arrive | live |
| AGT-22 | The agent's read appears once in the endpoint's structured log | live |
| AGT-23 | An unsatisfiable request yields no invented products | live |

## Out of scope

- The application's call to the agent (`e04s02`) and any UI (`e04`).
- Production networking, private endpoints, or a permanent public host: development and test only.
- CI/CD for the agent beyond the build job.

## Risks

- **A live test that fails the suite without credentials.** AGT-20 … AGT-23 must **skip cleanly**, the
  same opt-in pattern as `RealTenantTests`, or the hermetic baseline is lost.
- **The tool description driving selection.** On the H1 path, if the model calls the tool with a
  guessed filter, the imported schema's descriptions are the only mitigation.
- **A secret in the container.** A-2 needs the Auth0 client credential; it belongs in configuration or
  a connection, never in the image.
- **Region and model support.** Tool support requires both; a region that lacks it invalidates the
  deployment rather than the code.

## Acceptance criteria

- AGT-20 … AGT-23 pass with credentials present and skip without them.
- The endpoint's log records the agent's read, and the refusal is observed, not assumed.
- No tracked file and no image layer contains a credential.
- `specs/verifications/e02s07-after.md` records the image tag, agent version and configuration names.
