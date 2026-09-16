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

