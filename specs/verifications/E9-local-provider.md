# E9 verification — the local identity provider and the builder's gate

`tests/CoreRentalNet.E2E.LocalProvider`, started only by the browser suite, plus
`tests/CoreRentalNet.E2E/Flows/AuthenticationTests.cs`. The rest of the browser suite is unaffected.

## What was built

- A **minimal OpenID Connect provider in the repository**: discovery, JWKS, `/authorize` with a
  PKCE challenge and a one-line account chooser, an authorization-code token endpoint issuing a
  signed ID token and access token, `/userinfo`, and an end-session endpoint. It is a demonstration
  of the protocol, not a provider.
- **A second host in the browser suite**, with identity on and pointed at the provider through a new
  `Auth0:Authority` setting. The guest suite still runs its own host with identity off, so the
  additive promise (ADR-0016) is exercised on every run, not asserted once.
- **A guard that fails the boot** when the provider is asked to run outside Development, and a test
  that launches it in Production and asserts it exits non-zero (AUTH-18).
- For the gate itself, the app requests the API audience and the permission as a scope, keeps the
  access token in the session (`WithAccessToken` / `SaveTokens`), lifts its `permissions` claim into
  the identity, and derives the role from a permission ending `:role` (`RoleClaims`, ADR-0022).

## What the browser tests observe

| Account | Access-token permissions | Builder | Header link | Profile role |
|---|---|---|---|---|
| Dewi Reader | `read:catalog`, `manager:role` | opens | shown | `Manager` |
| Bagus Guest | none | `/access-denied` | hidden | not provided |
| Citra Reversed | `catalog:read`, `supervisor:role` | `/access-denied` | hidden | `Supervisor` |

The permission is the one configured in `appsettings.json` (`Authorization:CatalogRead`), and the
reversed words are refused on purpose: a substring or set-shaped check would pass them, and the test
exists so the difference is stated rather than assumed. Citra's role proves the role is read from
the access token — she carries no role in the ID token, so `supervisor:role` is the only place it is
stated.

## The real tenant (opt-in, E9-T7)

`RealTenantTests` drives the same sign-in against the developer's tenant, skipped unless
`CORERENTAL_TENANT_EMAIL` and `CORERENTAL_TENANT_PASSWORD` are in the environment. Run against the
tenant on **2026-09-13** with the account supplied for the check:

| | |
|---|---|
| Authentication | **succeeds** — the authorization-code handshake with PKCE, and the app issues its cookie |
| The account | name `budi.manager`, email `budi.manager@mailinator.com` |
| Access token | `aud` = the API **and** `/userinfo`; `scope` = `openid profile email read:catalog`; `permissions` = `["manager:role", "read:catalog"]` |
| Profile role | `Manager`, read from `manager:role` in the access token |
| `GET /builder` | **opens, and the canvas becomes interactive** |

The token is the evidence, not an assumption:

```json
{ "iss": "https://dev-ov6cn5bhs4xc00tr.us.auth0.com/",
  "aud": ["https://0z1ZZMu6afVqQc1iPI9H.periang.local",
          "https://dev-ov6cn5bhs4xc00tr.us.auth0.com/userinfo"],
  "scope": "openid profile email read:catalog",
  "permissions": ["manager:role", "read:catalog"] }
```

### What the first run of this slice got wrong

The gate originally shipped requiring `catalog:read` and was refused. Permission `catalog:read` is
not in the token — the tenant issues **`read:catalog`**. The application's read path was correct
throughout; the gate simply held a value the tenant never issued. Both earlier readings — "the
tenant has no API" and "the permission is `catalog:read`" — were wrong, and the opt-in test is what
corrected them. The decision is recorded in **ADR-0022**, which supersedes ADR-0021.

## The bugs this found

1. **Sign-out reached the real tenant.** With real Auth0 credentials in the developer's
   `appsettings.Local.json`, sign-out left the application for
   `https://{tenant}.auth0.com/v2/logout` — a third-party request from a suite whose value is
   hermeticity, and the one place a stray domain setting could leak. The Auth0 wrapper customises
   sign-out from the tenant domain; when an explicit authority is configured that customisation is
   now bypassed, so sign-out follows the authority's own discovery document.
2. **The provider dropped the sign-out state.** The OpenID Connect handler protects its properties
   into a `state` it sends to the end-session endpoint and expects back. The provider forwarded only
   the return address, so the application landed on a blank `/signout-callback-oidc`. Real providers
   echo `state`; the provider now does too.
3. **A refused account was already correct, and the test was wrong.** The application landed on
   `/access-denied?returnUrl=%2Fbuilder`; the test asserted the bare path. The route carries the
   original request, which is worth keeping.

## Honest limitations

- The provider is test infrastructure written here, so it can be wrong in the same way the
  application is wrong (ADR-0020). It is kept to the smallest flow the application uses and the
  assertions are about what a customer sees.
- It signs with a key generated at boot, per process. Nothing is reused and nothing is trusted
  across runs.
- The real tenant keeps its own opt-in test (E9-T7); this one proves the wiring and the tenant, but
  the tenant's permission name is part of the deployment contract (ADR-0022) and a rename there
  requires the matching configuration change here.

## Test counts

| Layer | Tests |
|---|---|
| Non-browser (unit, integration, architecture, host) | 425 |
| Browser (hermetic) | 98 |
| Browser (opt-in, real tenant) | 1 — skipped without credentials, passing with them |
| **Total** | **524** |
