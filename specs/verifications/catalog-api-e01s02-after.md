# e01s02 — catalog bearer auth — verification

Story: **e01s02 — Only an entitled machine reads the catalog** (5 BCPs, P0).
Branch: `catalog-api`, worktree `/Users/mac/Documents/AI Workspace/catalog-api`.

The endpoint now requires the application's existing `read:catalog` entitlement, presented as a
bearer token, and stays open where no identity provider is configured.

## Baselines

| Baseline | Before | After | Result |
|---|---|---|---|
| BUILD | 0 warnings, 0 errors | 0 warnings, 0 errors | unchanged |
| NONBROWSER | 512 passed, 0 failed | 519 passed, 0 failed | +7 (3 policy unit, 4 HTTP authorization) |
| BROWSER | 103 passed, 1 skipped | 103 passed, 1 skipped | unchanged |

## Test matrix coverage

| ID | Scenario | Where it is proven |
|---|---|---|
| API-08 | Identity configured, no token → 401 | `CatalogApiAuthorizationTests.Without_a_token_it_refuses_with_401` |
| API-09 | Valid token without `read:catalog` → 403 (including a value that merely contains it) | `CatalogApiAuthorizationTests` (two facts) |
| API-10 | Valid token with `read:catalog` → 200 and the catalogue | `CatalogApiAuthorizationTests.With_the_configured_permission_it_answers_with_the_catalogue` |
| API-11 | No provider configured → open (200) | `CatalogApiEndpointTests` (all eight, identity unconfigured) and `CatalogApiPolicyTests` (the policy names no scheme) |

## What changed

- `Host/Infrastructure/CatalogApiPolicy.cs` — the API policy's name in one place (`CatalogApiRead`).
- `Host/Composition/CatalogApiAuthentication.cs` — registers the JWT bearer scheme, from the same
  Auth0 authority and audience identity already uses, **only** when a provider is configured. The
  cookie stays the default scheme; bearer is named by the API policy alone.
- `Host/Composition/AuthorizationRegistration.cs` — `AddCatalogApiAuthorization` adds the API policy
  carrying the **same** `CatalogReadRequirement`, naming the bearer scheme only when one is registered.
- `Host/Controllers/CatalogController.cs` — `.RequireAuthorization(CatalogApiPolicy.Name)`.
- `Host/Program.cs` — calls the two new registrations.
- `Directory.Packages.props`, `CoreRentalNet.Host.csproj` —
  `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.12, pinned.
- Tests — `CatalogApiPolicyTests`, `CatalogApiAuthorizedFactory`, `CatalogApiTestHandler`,
  `CatalogApiAuthorizationTests`.

## Finding: the suite was not hermetic

The first run of the open-case tests returned **401**. The cause was not the policy: the Host has a
`UserSecretsId`, and the Development environment loads both the developer's **user secrets** and
`appsettings.Local.json` — the latter last, so it wins over anything a test injects. On this machine
that configured a real Auth0 tenant, so identity was configured and the policy demanded a token. The
browser suite documents the same hazard for its guest host.

Both in-process factories now run in the **`Testing`** environment, which loads neither source, so
identity is decided by the test. The same hazard would affect any future host test, so it is worth
keeping in mind beyond this story.

## Note on what the HTTP tests swap

The composition root reads identity settings while `Program` runs, before a test can add
configuration, so the authorized HTTP tests replace the two singletons the requirement handler reads
and re-point the policy at a test scheme. The **production** bearer wiring is covered by
`CatalogApiPolicyTests` (the policy names `JwtBearer` exactly when identity is configured) and by the
build; the end-to-end JWT validation is not exercised in-process.
