# Response contract for the catalogue API — plan

**Status: plan, not approved. Nothing here is implemented.**
Asked: the catalogue endpoint's responses are not detailed enough for success or for troubleshooting.
Cross-check against Microsoft documentation, the guidelines and the reference repositories, then propose
the change.

## What I read

| Source | What it says |
|---|---|
| [Microsoft Azure REST API Guidelines](https://github.com/microsoft/api-guidelines/blob/vNext/azure/Guidelines.md) — `microsoft/api-guidelines`, **23,333★, actively maintained** | Collections: "**DO** structure the response to a list operation as an object with a top-level array field containing the set (or subset) of resources", example `{"value":[…],"nextLink":"{opaqueUrl}"}`. "**YOU SHOULD** support paging today if there is ever a chance in the future that the number of items can grow to be very large" — "**NOTE: It is a breaking change to add paging in the future**". Errors: a structured error with `code`, `message`, `target`, `details[]`, `innererror`. "**DO** document the service's top-level error code strings; they are part of the API contract." "**YOU MAY** add additional properties … so customers don't resort to parsing your error message" — they "exist to help customers self-diagnose". |
| [Handle errors in ASP.NET Core APIs](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0) (.NET 10) | For controllers: "An *error result* is defined as a result with an HTTP status code of 400 or higher. For web API controllers, MVC transforms an error result to produce a `ProblemDetails`. **The automatic creation of a `ProblemDetails` for error status codes is enabled by default.**" Three ways to configure it: the problem-details service, a custom `ProblemDetailsFactory`, or `ApiBehaviorOptions.ClientErrorMapping`. `AddProblemDetails` + `UseExceptionHandler` + `UseStatusCodePages` are what make `ExceptionHandlerMiddleware` and `StatusCodePagesMiddleware` emit problem details. `CustomizeProblemDetails` is where extensions are added. |
| [Customize problem details](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0#customize-problem-details-with-customizeproblemdetails) | `AddProblemDetails(options => options.CustomizeProblemDetails = …)` is the documented hook for adding fields to every problem response. |
| [Handle errors in ASP.NET Core — problem details](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0#problem-details) | The documented non-development 500 body is `{"type":…,"title":"An error occurred while processing your request.","status":500,"traceId":"00-b644…-00"}` — the framework puts a `traceId` in it. |
| [UseStatusCodePages](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0#client-and-server-error-responses) | "can be configured to produce a common body content, **when empty**, for all HTTP client (400-499) or server (500-599) responses" — including routing errors such as 404. |
| [jasontaylordev/CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture/blob/main/src/Web/Infrastructure/ProblemDetailsExceptionHandler.cs) — **20,556★** | `ProblemDetailsExceptionHandler : IExceptionHandler` maps known exceptions to **RFC 9110-compliant `ProblemDetails`**, using the RFC section as `type` (`https://tools.ietf.org/html/rfc9110#section-15.5.5` for 404) with `status`, `title`, `detail`; unrecognised exceptions fall through to the default middleware. Registered with `AddExceptionHandler<>`. |
| [Same repo — `ApiExceptionOperationTransformer`](https://github.com/jasontaylordev/CleanArchitecture/blob/main/src/Web/Infrastructure/ApiExceptionOperationTransformer.cs) | Adds the standard error responses to **every operation in the OpenAPI document**, adding `401`/`403` only to operations carrying `IAuthorizeData` metadata. |

**One tension to name.** Microsoft's *guidelines* prescribe `{"error":{"code":…,"message":…}}`; ASP.NET
Core's *framework* produces RFC 9457 problem details by default. Following the guidelines literally
would mean a second error shape beside the one MVC already writes. The plan keeps problem details (the
standard, and what the framework and the OpenAPI tooling understand) and satisfies the guidelines'
actual requirement — a **documented, stable error code that is part of the contract** — with an
extension field.

## What the endpoint returns today — measured, not assumed

Run against a live host on `127.0.0.1:5177`, identity off:

| Case | Status | Body (measured) |
|---|---|---|
| Success | 200 | `[{"sku":"DSK6VE4Q6KEY","name":"Canggu Bamboo","category":"desk",…}]` — a **bare array** |
| Bad filter | 400 | `application/problem+json`: `{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"category":["Unknown category 'sofa'. Use one of: chair, desk, accessory."]},"traceId":"00-d97e3d7c92baa749be29ca709b428b06-2652f5be5cb6e816-00"}` |
| No token | 401 | **empty body**, no content type |
| Wrong permission | 403 | **empty body**, no content type |
| Unknown route | 404 | **empty body** (`Content-Length: 0`) |
| Wrong verb | 405 | **empty body** |
| Unhandled exception (production) | **404** | **empty body** |

The last row is not a typo. `ApplicationPipeline` configures `UseExceptionHandler("/error")`, and
**nothing serves `/error`** — measured: `GET /error` answers `404 Not Found`, `Content-Length: 0`. So in
production an unhandled exception is reported to the caller as a 404 with no body: the wrong status,
and nothing at all to diagnose with.

The 400 is the one response already worth keeping. Note it carries a `traceId` — I expected it would
not, and measured that it does (MVC's problem-details factory adds it). That is the standard the rest
of the responses should meet, not an aspiration.

## Findings, ranked

| # | Finding | Severity |
|---|---|---|
| **R1** | **The 500 path is broken.** An unhandled exception in production is answered `404` with an empty body, because the configured error path serves nothing. Wrong status **and** no diagnosis — the worst of both. | **HIGH** |
| **R2** | **A bare array is the one collection shape that cannot gain paging or a count later without a breaking change**, which the Microsoft guidelines call out explicitly. `{"value":[…]}` costs nothing now and removes that trap. | **HIGH** |
| **R3** | **401, 403, 404 and 405 return empty bodies.** A refused caller cannot tell an authorization failure from a typo, and none of them carries a trace id to quote in a report. | **MEDIUM** |
| **R4** | **No machine-readable error code.** The guidelines make the code strings part of the contract; today a caller wanting to branch must parse prose. | **MEDIUM** |
| **R5** | **The document does not describe the errors.** Only `200` and `400` are declared, and neither is typed as problem details, so a generated client has no error model. | **MEDIUM** |
| — | The 400 is already correct: problem detail, named allowed values, trace id. | keep |

## The plan

### 1. Success: a collection response, not a bare array

```json
{
  "value": [ { "sku": "DSK6VE4Q6KEY", "name": "Canggu Bamboo", "category": "desk", "…": "…" } ],
  "count": 62
}
```

`value` is the guidelines' own field name. `count` is what actually came back — the same number the
request log already records, and the field that makes "is this everything?" answerable without
counting the array.

**Paging is deliberately not added.** 62 products is not a large collection and inventing `nextLink`
now would be speculative (`CONVENTIONS.md`: no speculative abstraction). The envelope is precisely what
makes it *addable without a breaking change* later — which is the guidelines' concern, addressed
without paying for paging today.

### 2. Errors: one shape for every non-2xx on the API

Every error the API can produce answers `application/problem+json` with the same members, so a caller
writes one parser:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "The request could not be answered.",
  "status": 400,
  "detail": "One of the filters is not one this catalogue publishes.",
  "instance": "/api/catalog",
  "code": "catalog.unknown_filter",
  "traceId": "00-d97e3d7c…-00",
  "errors": { "category": ["Unknown category 'sofa'. Use one of: chair, desk, accessory."] }
}
```

- `type` — the RFC 9110 section for the status, as the reference repository does.
- `code` — **the contract field**: stable, documented, and what a caller branches on instead of prose.
  Proposed vocabulary: `catalog.unknown_filter`, `catalog.unauthenticated`, `catalog.not_permitted`,
  `api.unknown_route`, `api.method_not_allowed`, `api.unhandled`.
- `traceId` — the framework's, already present on the 400; guaranteed everywhere by
  `CustomizeProblemDetails`, and the one string that lets a caller's report be matched to a server log.
- `errors` — the per-field detail, already produced by the model state.
- `detail` — what a human reads; **never** an exception message or a stack trace.

### 3. Wiring: three framework pieces, scoped so a browser still gets a page

```csharp
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
    context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    context.ProblemDetails.Extensions["code"] = ApiErrorCode.For(context);
});

app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), api =>
{
    api.UseExceptionHandler();   // AddProblemDetails supplies the fallback body
    api.UseStatusCodePages();    // fills the empty 401/403/404/405 bodies
});
```

- `UseExceptionHandler()` with **no path** — `AddProblemDetails` supplies the response. This replaces
  `UseExceptionHandler("/error")`, whose target does not exist (R1). The developer exception page still
  runs first in Development, as documented.
- `UseStatusCodePages()` — this is what gives an empty 401/403/404/405 a body. Microsoft documents it
  as acting "when empty", which is exactly our case.
- **Both are scoped to `/api` on purpose.** This is a Blazor application: a browser navigating to a bad
  URL must still get a page, and Blazor has its own error UI for the circuit. A global
  `UseStatusCodePages` would answer page requests with JSON, which would be a worse defect than the one
  being fixed. The non-API branch keeps a **real** error page — `UseExceptionHandler("/error")` is only
  a fix once something serves `/error`.

### 4. Document it, so a generated client has an error model

- `[ProducesResponseType<ProblemDetails>]` for 401, 403, 404, 405 and 500 alongside today's
  `[ProducesResponseType<ValidationProblemDetails>]` for 400, and the collection envelope as the 200
  type.
- An `IOpenApiOperationTransformer` adding the standard error responses to every operation and gating
  401/403 on `IAuthorizeData` metadata — the pattern the 20,556★ reference uses, and one that is now
  available to us because the endpoint is a controller with an `[Authorize]` attribute.
- `specs/` gains the error-code vocabulary as a contract document.

### 5. Tests and verification

New matrix ids, from the existing numbering:

| ID | Assertion |
|---|---|
| API-17 | the 200 body is an envelope whose `value` is the catalogue and whose `count` matches its length |
| API-18 | an unknown filter is a 400 whose `code` is the documented code and whose `errors` name the values |
| API-19 | an unknown route under `/api` is a 404 **problem detail**, not an empty body, with a `traceId` |
| API-20 | a wrong verb on a real route is a 405 problem detail |
| API-21 | 401 and 403 are problem details with their documented codes |
| API-22 | an unhandled exception is a **500** problem detail carrying a `traceId` and **no exception text** |
| API-23 | a **page** request for an unknown route is still HTML, not JSON (the scoping rule) |

Non-vacuity: each of API-19…API-22 must fail when the corresponding registration is removed — the same
method used for the controller change, where removing the binder failed exactly the tests that depend
on it. API-22 additionally gets a security review, because "no exception text in the response" is a
disclosure rule, not a formatting rule.

## Decisions I need

1. **Envelope field names** — `value` + `count` (recommended: `value` is the guidelines' name), or
   `items` + `total`?
2. **Paging now or envelope only?** Recommended: envelope only, documented, paging addable later
   without a break.
3. **The `code` vocabulary** — is `catalog.unknown_filter` style (dot-separated, namespaced) the shape
   you want, and are those six the right codes?
4. **Scoping rule for the error bodies** — path prefix `/api` (recommended, simplest and testable) or
   `Accept`-header negotiation?

## What breaks, honestly

- The bare array is the current contract: `CatalogApi.http`, the Swagger example, `API-03`…`API-06` and
  the funnel tests all read it. They change with it. **There are no external consumers and no
  versioning**, so this is the cheapest moment this change will ever be — and the envelope is what stops
  this class of break recurring.
- `AccountController`'s redirects and the Blazor pages are untouched; the scoping in §3 is what keeps
  that true.
- `UseExceptionHandler` in a Blazor Server app should be re-checked against the circuit's own error
  handling during implementation, and the browser suite is where that shows up.

## Not in this change

API versioning, rate-limit headers, `ETag`/concurrency on the catalogue read, and `nextLink` paging —
all named by the guidelines, none of them needed by a read-only catalogue of 62 items today. Recorded
here so the omissions are deliberate rather than forgotten.
