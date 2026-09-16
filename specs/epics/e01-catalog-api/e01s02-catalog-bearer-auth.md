# e01s02 — Only an entitled machine reads the catalog

**type:** feat
**risk:** P0
**context:** infra
**bcps:** 5
**status:** failing

## Context

The endpoint from e01s01 is open only because no identity provider is configured. Once one is, a
caller must prove it carries `read:catalog`. This story adds JWT bearer authentication for the API and
a `catalog-api` policy that requires the same `CatalogReadRequirement` the browser pages use, naming
the bearer scheme. The browser's cookie scheme and `CatalogPolicy` are untouched.

## Requirements

#### ADDED: A machine caller presents a bearer token carrying read:catalog

When an identity provider is configured, `GET /api/catalog` requires a valid bearer token whose
principal carries the configured claim (`permissions=read:catalog`). No token → `401`; a valid token
without the claim → `403`; a valid token with it → the e01s01 behaviour. When no provider is
configured the endpoint is open, because `CatalogReadAuthorizationHandler` already opens the catalog
when there is nobody to authenticate.

#### MODIFIED: The catalog policy gains a second, scheme-specific entry point

**Before:** one policy (`catalog`) on the default scheme (the Auth0 cookie), used by the Blazor pages.
**After:** the same `CatalogReadRequirement` is also carried by a `catalog-api` policy that names the
JWT bearer scheme, and that scheme is added to the policy only when identity is configured. The
`catalog` policy and the cookie flow are unchanged.

## Steps

1. Add `Microsoft.AspNetCore.Authentication.JwtBearer` [OK] to `Directory.Packages.props` and the Host project. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
2. Add `Host/Composition/CatalogApiRegistration.cs`: when identity is configured, register JWT bearer from the existing Auth0 authority and audience and add the `catalog-api` policy (requirement + bearer scheme); when it is not, add the policy with no scheme. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
3. Apply `RequireAuthorization(CatalogApiPolicy.Name)` to the endpoint. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
4. Call the registration from `Program.cs` beside `AddCatalogAuthorization`. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
5. Unit tests for the policy wiring: the bearer scheme appears on the policy exactly when identity is configured (CatalogApiPolicyTests). → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~CatalogApiPolicyTests"`
6. In-process HTTP tests API-08 … API-11, using a test authentication scheme that can produce a principal with or without the claim (CatalogApiAuthorizationTests). → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~CatalogApiAuthorizationTests"`
7. Re-run the three baselines. → verify: `dotnet test CoreRentalNet.sln --filter "FullyQualifiedName!~CoreRentalNet.E2E" --nologo`

## Verification Script (Step-by-Step)

1. Configure a provider in development settings and start the host.
2. `curl -s -o /dev/null -w '%{http_code}' "http://localhost:5000/api/catalog"` → 401.
3. Request a client-credentials token from the Auth0 M2M application, then `curl -s -o /dev/null -w '%{http_code}' -H "Authorization: Bearer $TOKEN" "http://localhost:5000/api/catalog"` → 200.
4. Request a token for a client without `read:catalog` → 403.
5. Remove the provider configuration, restart, repeat the unauthenticated call → 200.

## Test matrix

| ID | Scenario | Level |
|----|----------|-------|
| API-08 | Identity configured, no token → 401 | http |
| API-09 | Valid token, no `read:catalog` claim → 403 | http |
| API-10 | Valid token with the claim → 200 | http |
| API-11 | No provider configured → 200 (open) | http |

## Out of scope

- API keys, Entra managed identity, anonymous production access, rate limiting.
- Any change to `CatalogPolicy`, the cookie scheme, or the Blazor flow.

## Risks

- **Scheme registration is conditional.** A policy that names an unregistered scheme throws when
  authorization runs; the bearer scheme must be added only when identity is configured, and the tests
  must cover both branches.
- **Bearer must not become the default scheme.** If it does, the Blazor circuit's cookie sign-in breaks;
  the API policy names its scheme explicitly.

## Acceptance criteria

- API-08 … API-11 pass.
- `CatalogReadAuthorizationHandlerTests`, `CatalogAuthorizationPolicyTests` and the E2E authentication
  flow are unchanged.
