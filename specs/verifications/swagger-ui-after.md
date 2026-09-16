# Swagger UI with Auth0 — verification

Adds the swagger-ui page in development, authorised by Auth0 through the authorization-code flow with
PKCE, so the catalogue endpoint can be tried from a browser without a request file. Development only.

## Baselines

| Baseline | Before | After | Result |
|---|---|---|---|
| BUILD | 0 warnings, 0 errors | 0 warnings, 0 errors | unchanged |
| NONBROWSER | 529 passed, 0 failed | 536 passed, 0 failed | +7 |
| BROWSER | 103 passed, 1 skipped | 103 passed, 1 skipped | unchanged |

## Test matrix

| ID | Scenario | Where it is proven |
|---|---|---|
| API-14 | The document declares an `oauth2` authorization-code scheme whose authorization and token addresses come from the identity configuration, with `read:catalog` among its scopes | `CatalogOpenApiTests.The_document_declares_how_a_caller_obtains_a_token` |
| API-14 | The endpoint requires that scheme | `CatalogOpenApiTests.The_endpoint_requires_the_scheme_the_document_declares` |
| API-15 | The page is served in development, configured from the identity settings | `SwaggerUiTests.The_page_is_served_in_development`, `…The_page_is_configured_from_the_identity_settings` |
| API-15 | A named public client is used for the page and the sign-in client is not | `SwaggerClientTests.Naming_a_client_for_the_page_leaves_the_sign_in_client_alone` |
| API-15 | Neither the page nor the document is served outside development | `CatalogApiEndpointTests.The_swagger_ui_is_not_served_outside_development`, `…The_openapi_document_is_not_served_outside_development` |
| API-16 | The policy that admits swagger-ui's inline code applies to swagger's paths only | `SwaggerUiTests.The_documentation_is_served_under_its_own_policy_and_nothing_else_is` |

## Real-host evidence

Development: `/swagger/index.html` **200**, `/swagger/oauth2-redirect.html` **200**, `/openapi/v1.json`
**200**, `/api/catalog` **401** (unchanged, the gate).

With `Auth0:Audience` and `Auth0:Scope` configured, the page's own configuration carries
`scopes = [openid, profile, email, read:catalog]`, `additionalQueryStringParams.audience`, and PKCE
on; it sets no `oauth2RedirectUrl`, so swagger-ui derives it from the page's address.

Production: `/api/catalog` **200**, `/swagger/index.html` **404**, `/openapi/v1.json` **404**.

The relaxed policy appears on `/swagger` and not on `/` (checked per response).

## The Auth0 side, which is not code

For **Authorize** to produce a token the gate accepts, the deployment must have:

1. **`Auth0:Audience`** set to the API identifier. Without it Auth0 issues a token with no
   `permissions` claim and the endpoint answers `403`. This is the repository's own documented finding
   (README, "The builder is gated by the `read:catalog` permission").
2. **`read:catalog` in `Auth0:Scope`**, because a per-app-authorization policy issues a permission only
   when the login asks for it as a scope.
3. The Swagger callback registered in the client's **Allowed Callback URLs**:
   `{address}/swagger/oauth2-redirect.html` — for the pinned development address,
   `http://localhost:5199/swagger/oauth2-redirect.html`.

Everything else is reused: the client id, the scope string, the audience and the authority all come
from `Auth0:*`, and no client secret is involved because PKCE is used.

### The client id, when reuse will not do

The client id defaults to `Auth0:ClientId`, so a deployment that can authorize with the application's
own client configures nothing. But PKCE is documented for clients that *cannot* hold a secret -
single-page and native applications - while a regular web application is a confidential client, and a
provider may refuse the exchange without a secret for it. `Swagger:ClientId` names a public client for
this page instead, and changes nothing else: the application's sign-in client is untouched.
`SwaggerClientTests` proves the override takes effect and that the sign-in client does not appear on
the page.

## One thing the smoke test exposed

Run from a worktree, only user secrets apply, and on this machine those set a domain and client id but
neither an audience nor a `read:catalog` scope — so the page came up with no audience. Running from the
main repository in Development loads `appsettings.Local.json`, which does carry both. Worth knowing
before concluding the flow is broken: the page reflects whatever `Auth0:*` says.

## What changed

- `Swashbuckle.AspNetCore.SwaggerUI` 10.2.3, pinned.
- `OpenApiRegistration` — registers the document and its scheme transformer.
- `Auth0SecuritySchemeTransformer` — declares the scheme from the identity configuration.
- `SwaggerUiRegistration` — maps the page in development and configures its OAuth settings from the
  same configuration.
- `DocumentationPath` — the route, named once, because the security headers treat it as a boundary.
- `SecurityHeadersMiddleware` — the policy is now chosen per request: the strict one everywhere, and a
  narrower exception for swagger's paths **in development only**.
- Tests — `SwaggerUiTests`; two document tests; a not-served guard.

## Notes

- The CSP exception is the one change to the security posture. It is development-only, scoped to
  swagger's paths, asserted by a test that the application's own pages keep the strict policy, and it
  keeps `frame-ancestors 'none'` and `object-src 'none'`.
- `EnableValidator(null)` disables swagger-ui's validator badge, which fetches a third party the policy
  forbids; leaving it on would only draw a broken image.
- The redirect address is deliberately not configured: swashbuckle's own `index.js` defaults it to
  `oauth2-redirect.html` beside the page, so it is right on any host and port.

## Correction, after the independent review

Two of this record's claims were wrong, and the way they were wrong is the point:

- **The page did not work.** It was never told which document to load, so it fetched swashbuckle's
  default `v1/swagger.json` — which this application does not serve. Measured: **404**. The page
  rendered and showed no operations. Every check in this record passed anyway, because they verified
  that things *served* rather than that the page could *read* them.
- **The Authorize button could not have completed.** The relaxed policy kept `connect-src 'self'`, and
  the authorization-code exchange is a fetch from the browser to the provider's token endpoint.

Both are fixed, with a browser test that fails when the fix is removed. See
`swagger-ui-review-response.md`.
