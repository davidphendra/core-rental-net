# Security review — catalogue API (epic e01)

Scope: the branch `catalog-api` (`acc9733..6bdf991`) — the read-only catalogue endpoint, its bearer
authorization, and its call log.

Method: manual tracing of the diff. The `security-review` skill is AI analysis and ships no script in
this repository, so the review was performed against the same questions it asks: data flow, injection,
authorization bypass, secrets, and unsafe deserialization.

## Verdict: PASS — no HIGH findings

| ID | Severity | Area | Finding | Handling |
|---|---|---|---|---|
| S1 | LOW | Availability | No rate limiting on the endpoint. | Accepted in the scope: the caller is one server-side agent, the catalogue is 62 in-memory rows, and a request measures under 5 ms (NFR evidence). Revisit if the endpoint is ever exposed to browsers. |
| S2 | LOW | Log forging | `search` is logged as a structured property; a value containing a newline would render an extra line in a plain-text sink. | Accepted. The value is a structured property, never concatenated into a message template, and the sink is the host's console. |
| S3 | LOW | Input size | `search` has no length cap. | Accepted in the scope: `string.Contains` over 62 in-memory records. |
| S4 | INFO | Operability | No health endpoint. | Pre-existing, outside this epic. |

## Checked and clean
- **Injection.** No SQL, shell, file path or reflection is reachable from the request. The only
  arithmetic is `string.Contains` with `OrdinalIgnoreCase` over in-memory records; the catalogue is a
  snapshot loaded at start-up, so there is no query to inject into.
- **Authorization.** The endpoint requires the same `CatalogReadRequirement` the pages use. The API
  policy names the bearer scheme and no other, so a browser cookie cannot authorise the API —
  asserted by `CatalogApiPolicyTests`. With no provider configured the catalogue is open, which is the
  existing, documented rule for the store page and the direction that keeps a demo usable.
- **Authentication.** Bearer validation is configured from the same authority and audience identity
  already uses. A real host with identity configured answered `401` to both a missing and a malformed
  token (see `catalog-api-verify.yaml`).
- **Secrets.** None added. No secret in configuration; the test factories use a fake domain and no
  client secret.
- **Information disclosure.** The body is the module's published `ProductView` — public catalogue
  data. No token, no customer, no draft. The endpoint has no write surface.
- **Logging.** Caller (`sub`), the three filters, and the count. No token, no response body, no
  customer identity beyond the subject.
- **CORS.** None configured, and the caller is not a browser.
- **Response headers.** The application's existing security headers apply to API responses
  (`Content-Security-Policy`, `X-Content-Type-Options`, `Referrer-Policy`) — verified on a real host.

## Addendum — after the review response (`9122b65`)

`CatalogApiAuthentication.cs` changed after this review, answering findings S2–S4:

- the audience requirement is now stated explicitly (`TokenValidationParameters.RequireAudience = true`)
  beside `options.Audience`, rather than relying on the framework default;
- `RequireHttpsMetadata` is `false` only in development, and only when a non-tenant authority is named;
- the redundant `MetadataAddress` assignment was removed (the handler derives it from the authority).

The assessment is unchanged: **no HIGH finding**. Each change tightens what a token must satisfy rather
than relaxing it. A real host with a provider configured and no audience answered `401` to both a
missing and a malformed token (the first attempt at this fix dropped the bearer scheme and answered
`302`, a login redirect; that shape was rejected).

## Addendum — the OpenAPI document (`8984cbb`)

Development now serves an OpenAPI document at `/openapi/v1.json`. Assessed:

- **Exposure.** Mapped only when `Environment.IsDevelopment()`; a real host confirms `404` in
  Production while `/api/catalog` still answers `200`. The document names the routes and shapes the
  application has, so it is a map handed to whoever asks — which is why it is not published anywhere it
  is not needed.
- **Authorization.** Anonymous by design: a description of the API is not the API, and it carries no
  catalogue data — product values are not in it, only their shapes.
- **Secrets.** It contains no configuration values, no keys and no credentials; it is generated from
  endpoint metadata and the serializer's schemas.
- **The request file** `CatalogApi.http` is a repository file, not a served route: it adds no HTTP
  surface. It does not embed a token (its `@token` is a placeholder).

No new finding; the assessment stands.

## Addendum — the documentation page's policy exception (`fix-review`)

The third change to the posture, and the largest: `/swagger` is served in development under a policy
that admits swagger-ui's inline script and styles, and that lets the browser reach the provider's
token endpoint.

**Scope.** Development only, and only for paths under `/swagger` — a case-insensitive prefix match, so
`/swaggerz` is not covered. `base-uri`, `object-src`, `frame-ancestors`, `form-action`, `default-src`,
`img-src` and `font-src` are byte-identical to the strict policy. The exception is *derived* from the
strict policy by substitution, so a directive added to one cannot be forgotten in the other, and a test
asserts the exact set of directives that differ.

**Why it cannot be narrower.** Two allowances are forced. swagger-ui builds its page from an inline
script carrying its configuration and from inline styles, so without `'unsafe-inline'` the page renders
blank — the failure this middleware exists to make visible. And the authorization-code exchange is a
fetch from the browser to the provider's token endpoint, which `connect-src` governs; without the
provider's origin the flow fails after the redirect. The origin is taken from the same
`IdentitySettings` the token URL comes from.

**Alternative considered and rejected.** swashbuckle lets the page be replaced
(`SwaggerUIOptions.IndexStream`), so a per-response nonce could be threaded from the middleware into
the generated page and named in `script-src`, avoiding `'unsafe-inline'`. Rejected: it moves a nonce
through two components for a development-only page, it must be recomputed per response, and it would
still need `connect-src` widened for the exchange — so it removes one word from one directive and adds
machinery to the request path. Recorded here rather than left implicit.

**Residual risk.** `'unsafe-inline'` applies to every response under the prefix, including the
application's own 404 for `/swagger/<anything>`. The relaxation is bound to the Development environment
and not to the request's host, so a development host bound to a non-loopback interface (`--urls
http://+:5199`) would serve it — with the configured client id and audience on the page. Both are
public values rather than secrets, and the guard is the same one that maps the page; the mitigation, if
it is ever wanted, is to relax only for loopback requests.


## Addendum — what the API's refusals say (`api-responses`)

The API now answers every refusal with problem details carrying a `code` and a `traceId`, and an
unhandled failure with a 500 rather than the 404-with-no-body it used to produce. Two rules were
applied, and both are disclosure rules rather than formatting ones.

**The response never carries the failure itself.** The 500's `detail` is the framework's own wording;
the exception's message and type never reach the caller. An exception message is written for a
developer reading a log and routinely names a path, a query or a value the caller was not entitled to
see — and an unhandled exception is exactly the case where nobody has decided what is safe to say.
Asserted, not assumed: `API-22` asserts the body does **not** contain the exception's message or its
type name.

**`traceId` is the framework's identifier, not a new one.** It is the W3C trace id the request was
logged under, only added when the framework has not already supplied it — so the identifier a caller
quotes is the one that will actually be found in the logs. It is not a secret: it identifies a request,
and the request's content is not in it.

**Residual risk.** The codes are a published contract, so they invite a caller to branch on them; a
future change to a code string is therefore a breaking change and must be treated as one. The `errors`
member of the 400 echoes the filter as it was sent, including a value the caller chose — that value is
the caller's own input, not server state, so it discloses nothing it did not already know.


## Addendum — one rule for four permissions (`e04s01`)

The entitlement mechanism was generalised so the AI section can have its own permission. One
`ClaimRequirement` now carries the claim it checks and what it means in a deployment with no identity
provider, one handler answers every requirement, and three policies are declared where there was one.
This is an edit to code this review had already covered, in an epic that otherwise has nothing to do
with authentication, so it is reviewed here rather than left implicit.

**What was preserved, and asserted rather than assumed.** The catalogue's own rule is unchanged: it
opens when no provider is configured, it requires an authenticated account and an **exact,
case-sensitive** claim match, it fails closed on a half-configured claim, and no claim type or value is
written into code. The tests that asserted these were carried across unchanged in substance — the
classes were renamed, the assertions were not.

**What is new, and is the reason the change was needed.** The AI section's two permissions answer
`Closed`: a deployment with no identity provider does **not** open them, where it does open the
catalogue. The asymmetry is the point. Reading the catalogue is served from memory and costs nothing to
allow, so opening it keeps an unconfigured demonstration usable; generating a suggestion spends model
calls and an external round trip, so opening it would make the demo an unmetered spend endpoint.

**Threats considered.**

- *A permission that opens by accident.* The behaviour is a value on the requirement rather than a
  branch in the handler, and each of the four policies passes it explicitly. A test asserts both
  behaviours against the same handler, so a handler that answered one of them for both would fail.
- *One handler drifting from another.* There is one handler. The alternative — a handler per permission
  — is four copies of an exact comparison, and four copies is four chances for three of them to be
  right.
- *A half-configured claim.* Unchanged and now asserted for both behaviours: a provider with an
  incompletely configured claim denies, whichever way the permission answers a missing provider.
- *A claim value leaking into configuration history.* The two new sections in `appsettings.json` name
  permission **strings** — public values, of the same kind the catalogue's already were — and no secret
  is added. `AUTH-15` still fails the build if a claim value reaches *code*.

**Residual risk.** The AI permissions are declared and tested but **nothing consumes them yet**: the
section they guard arrives with `e04s03`. Until then a misconfiguration of `AiBuilderRead` or
`AiBuilderPower` would be invisible, because no route or page asks. The mitigating fact is that both
are `Closed`, so the failure mode in the meantime is a section that cannot be reached rather than one
that can be reached by everyone.
