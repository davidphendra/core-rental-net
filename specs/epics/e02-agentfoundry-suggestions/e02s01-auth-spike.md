# e02s01 — A machine reads the catalogue through the agent's chosen path

**type:** spike
**risk:** P0
**context:** infra
**bcps:** 2
**status:** failing

## Context

Everything downstream rests on one behaviour Microsoft's documentation does not confirm: that a
Foundry `oauth2` connection performs the **client credentials** grant against Auth0. The documented
example shows a **consent link**, and the tooling reference states the OAuth2 custom-app connection
**"runs as the user"**, while the machine paths are a **static bearer** (Custom keys) or **Entra
managed identity** — and Entra is unavailable because this API is Auth0.

The deliverable is **a written answer, not production code**. The fallback is pre-committed so the
decision is not made under pressure later.

## Requirements

#### ADDED: a go/no-go on the machine path

A throwaway Foundry project, model deployment, Auth0 machine-to-machine client and `oauth2`
connection exist; a Toolbox OpenAPI tool built from the current `/openapi/v1.json` calls
`GET /api/catalog`; and the observed result is recorded as a go or a no-go with its evidence in
`specs/archive/spikes/SPIKE-auth0-client-credentials-foundry.md`.

**The pre-committed rule:**
1. If the connection performs **client credentials** → keep **H1**: the Toolbox OpenAPI tool stays.
2. If it demands **user consent** → take **A-2**: the container mints its own token. **Never** a stored
   bearer (A-1), whose failure mode is silent and time-based.
3. If it fails for an unrelated reason (region, tool support) → fix that and re-run.

## Zoom-Out

- **Module purpose:** none — this story changes no code. It exercises the endpoint `e01` built and
  the Auth0 tenant that already configures `read:catalog`.
- **Callers:** the Toolbox, on behalf of the hosted agent (`e02s07`).
- **Contracts to preserve:** the endpoint's request, response and refusal shapes and the
  `read:catalog` gate. The spike must not change them.

## Steps

1. Create the throwaway resource group and record its name. → verify: `az group show --name corerental-e03-dev --query name -o tsv`
2. Enable the `client_credentials` grant on the Auth0 machine-to-machine client and grant it `read:catalog`. → verify-script: see step 1 of the Verification Script.
3. Prove outside Foundry that the credential yields a token. → verify: `curl -s -X POST "https://$AUTH0_DOMAIN/oauth/token" -H 'content-type: application/json' -d "{\"grant_type\":\"client_credentials\",\"client_id\":\"$AUTH0_CLIENT_ID\",\"client_secret\":\"$AUTH0_CLIENT_SECRET\",\"audience\":\"$AUTH0_AUDIENCE\"}" | jq -r '.access_token' | wc -c`
4. Prove outside Foundry that the token reads the catalogue. → verify: `curl -s -o /dev/null -w '%{http_code}' -H "Authorization: Bearer $TOKEN" "$CATALOG_URL/api/catalog?view=compact"` → `200`
5. Provision the Foundry project and a model deployment whose **region and model** both support the OpenAPI tool. → verify-script: see step 2 of the Verification Script.
6. Create the `oauth2` connection pointing at Auth0. → verify: `azd ai connection create agentfoundry-auth0 --kind remote-tool --target "$CATALOG_URL" --auth-type oauth2 --authorization-url "https://$AUTH0_DOMAIN/authorize" --token-url "https://$AUTH0_DOMAIN/oauth/token" --client-id "$AUTH0_CLIENT_ID" --client-secret "$AUTH0_CLIENT_SECRET" --scopes "read:catalog"`
7. Expose the local host so the tool has something to call. → verify: `curl -s -o /dev/null -w '%{http_code}' "$CATALOG_URL/openapi/v1.json"` → `200`
8. Create the Toolbox and invoke a throwaway agent through it. → verify-script: see step 3 of the Verification Script.
9. Write the note with the go/no-go and the evidence. → verify: `test -f specs/archive/spikes/SPIKE-auth0-client-credentials-foundry.md`
10. Delete the throwaway Toolbox and agent; keep the resource group and the connection. → verify: `azd ai toolbox delete agentfoundry-catalog --yes`

## Verification Script (Step-by-Step)

1. In Auth0, confirm the client's **Grant Types** includes **Client Credentials** and that
   `read:catalog` is assigned to it.
2. In the Foundry portal, confirm the project's model deployment is in a region whose tool-support
   table says **Yes** for the OpenAPI tool, and that the model says **Yes** too. Both are required.
3. Invoke the throwaway agent with *"list the desks"* and observe (a) an answer naming real products
   and (b) exactly one `CatalogApiLog.Called` line in the host's console.
4. Open the run's trace and record **which grant the connection used** — authorization-code with a
   consent prompt, or client credentials.
5. Answer the go/no-go. If it is a no-go, state precisely where it failed — connection creation, token
   acquisition, header propagation, or the call — because that determines the fallback.

## Test matrix

| ID | Scenario | Level |
|----|----------|-------|
| — | This story proves the path; `e02s07` asserts it as AGT-20 … AGT-23 | live |

## Out of scope

- Any application change. `e03` owns the catalogue contract; `e02s02` owns the agent contract.
- The agent's instructions, workflow and container (`e02s02` … `e02s05`).
- Cost, quota and region sizing beyond what the model deployment needs.

## Risks

- **A consent link is the likely outcome.** That is why the fallback is pre-committed: A-2 keeps the
  endpoint's gate untouched and removes the expiry cliff.
- **Endpoint reachability.** Foundry cannot call `localhost`; a tunnel or a development host is
  required, and it must be recorded as development-only.
- **Secret handling.** The Auth0 client secret belongs in the connection and a password manager —
  never in a tracked file, a committed toolbox YAML, or the spike note.
- **Region and model support.** A region without the OpenAPI tool invalidates the spike for reasons
  unrelated to Auth0. Record the region.

## Acceptance criteria

- The spike note exists, states go or no-go, and carries the observed evidence.
- A recorded call reached `GET /api/catalog` and appears in the endpoint's structured log, **or** the
  note records exactly where it failed and confirms A-2 is viable.
- No tracked file contains a credential.
