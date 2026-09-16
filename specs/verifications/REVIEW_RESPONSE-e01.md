# Response to the epic review — e01 (catalogue API)

Reviewer: `cqrs-reviewer-agent` on `main..HEAD` (`acc9733..36c4bbc`). Verdict: sound, no HIGH.
Four findings and one documentation call. Every one is addressed below.

| ID | Sev | Disposition |
|---|---|---|
| C1 | LOW | **Fixed.** `specs/CQRS_DESIGN_LATEST.md` now names the HTTP machine consumer in the context table and shows its read path in the technical flow, and states that a delivery channel is not a model: the level stays L1 and no second read model was introduced. |
| S1 | MEDIUM | **Fixed.** `CatalogApiPolicyTests` now resolves `IAuthenticationSchemeProvider` and the `JwtBearerOptions` for the scheme, so the production registration is executed: removing `AddJwtBearer` fails three of the five policy tests instead of none. |
| S2 | MEDIUM | **Fixed, by making the requirement explicit.** See below. |
| S3 | LOW | **Fixed.** `RequireHttpsMetadata` is set false only in development, and only when a non-tenant authority is named. |
| S4 | LOW | **Fixed.** The metadata address is no longer written; the handler derives it from the authority, so the rule exists once. |

## S2 in detail, including a correction to my first attempt

The reviewer's rule — "do not fail open on a missing bound" — is right. My first fix was wrong: it
stopped registering the bearer scheme when no audience was configured. A real host then answered
**302**: with no scheme named, the authorization middleware challenges with the *default* scheme, the
browser cookie, and sends a machine caller to the sign-in page. Closed, but not the refusal the
contract promises.

The fact that decides the right fix, from the API reference: `TokenValidationParameters.RequireAudience`
defaults to **true**, and audience validation is skipped *only* when it is set to **false**
([MS Learn](https://learn.microsoft.com/dotnet/api/microsoft.identitymodel.tokens.tokenvalidationparameters.requireaudience)).
So the correct shape is:

- keep the bearer scheme registered **whenever there is a provider**, so a machine caller is always
  answered by the bearer challenge — `401` — and never by a browser redirect; and
- state the audience requirement **explicitly** (`RequireAudience = true`) beside `options.Audience`
  rather than relying on a default, so a deployment that names no audience refuses every token instead
  of accepting one on its issuer alone.

**Real-host evidence** (Development, provider configured, `Auth0:Audience` empty): `401` with no token,
`401` with a malformed token — where the first attempt returned `302`.

## Dismissed by the reviewer, acknowledged

The four `SERVICE_LOCATOR` flags (tests build a container on purpose), the two `CATCH_ALL` flags
outside the change set (both filtered), the `CatalogApiLog.Category` literal, and the two factories'
temporary-database duplication. No action; the reasons are in `AUDIT-e01.md` and the review.

## Still open

**A valid bearer token has not been presented to a real host.** That needs a machine-to-machine token
from the tenant — an operational act, not a code change. S1 removed the automated half of the gap.

## Baselines after the response

| Baseline | Before | After |
|---|---|---|
| BUILD | 0 warnings, 0 errors | 0 warnings, 0 errors |
| NONBROWSER | 523 passed | 525 passed (+2 policy tests) |
| BROWSER | 103 passed, 1 skipped | 103 passed, 1 skipped |
