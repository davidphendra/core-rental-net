# Aligning the API surface with ASP.NET Core practice

**Status: planning note, not approved. Nothing here is implemented.**
Requested: cross-check the current endpoint against Microsoft documentation and the highest-starred
reference repositories, say which differences matter, and propose the change.

## The ask

The catalogue endpoint is a `static class CatalogController` that maps a route with `MapGet`, and the
author would rather be on `ControllerBase` with `[Authorize]` on it.

## What I read

| Source | What it says |
|---|---|
| [APIs overview](https://learn.microsoft.com/aspnet/core/fundamentals/apis?view=aspnetcore-10.0#choosing-between-approaches) | "**Start with Minimal APIs** for new projects… **Consider controller-based APIs** if you need: model binding extensibility (`IModelBinderProvider`, `IModelBinder`), advanced validation (`IModelValidator`), application parts or the application model, OData." |
| [Minimal APIs quick reference — Authorization](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-10.0#authorization) | "Routes can be protected using authorization policies. These can be **declared via the `[Authorize]` attribute** or by using the `RequireAuthorization` method." `[AllowAnonymous]` likewise; [`[Authorize(AuthenticationSchemes = …)]`](https://learn.microsoft.com/aspnet/core/security/authorization/authorize-with-a-specific-scheme?view=aspnetcore-10.0) names schemes on endpoints, controllers and pages alike. |
| [Validation in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/validation?view=aspnetcore-10.0) (.NET 10) | `AddValidation()` validates query, header and body from `DataAnnotations` attributes and returns **"a 400 with details of the validation errors"**; shaped by `IProblemDetailsService`. Multi-assembly needs an `AddValidation()` extension per assembly. "The API isn't supported for MVC or Razor Pages." |
| [Create web APIs with ASP.NET Core — `[ApiController]`](https://learn.microsoft.com/aspnet/core/web-api/?view=aspnetcore-10.0#apicontroller-attribute) | The attribute's opinionated behaviours: attribute-routing requirement, automatic HTTP 400, binding-source inference, multipart inference, ProblemDetails for error status codes. |
| [Parameter binding in minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0#binding-failures) | A `TryParse` failure on a nullable route/query/header parameter is already **400** — before any validation package. |
| [Customize OpenAPI documents](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/customize-openapi?view=aspnetcore-10.0) | Security *schemes* are registered by a document transformer; security *requirements* are conditional per operation in an operation transformer, which reads endpoint metadata (the documented example skips `AllowAnonymous`). This is what the code already does. |
| `AllowedValuesAttribute` ([source](https://github.com/dotnet/runtime/blob/main/src/libraries/System.ComponentModel.Annotations/src/System/ComponentModel/DataAnnotations/AllowedValuesAttribute.cs), [resources](https://github.com/dotnet/runtime/blob/main/src/libraries/System.ComponentModel.Annotations/src/Resources/Strings.resx)) | Message: *"The {0} field does not equal any of the values specified in AllowedValuesAttribute."* It does **not** name the allowed values. |
| [jasontaylordev/CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture) — **20,556★, actively maintained** | Grouped minimal-API endpoint classes: `IEndpointGroup` with `static void Map(RouteGroupBuilder)`, group-level `RequireAuthorization()`, `[EndpointSummary]`, `TypedResults`, `MapEndpoints(assembly)` discovery. OpenAPI + **Scalar**. No MVC controllers. |
| [eShopOnContainers](https://github.com/dotnet-architecture/eShopOnContainers) — **24,315★, archived** | The classic controller shape: `[Route("api/v1/[controller]")] [ApiController] class CatalogController : ControllerBase`, `[FromQuery]` parameters, `[ProducesResponseType]`, `IActionResult`, `Ok(...)`/`BadRequest(...)`. |
| [eShopOnWeb](https://github.com/dotnet-architecture/eShopOnWeb) — **10,681★, archived** | Same era and same shape. |

## Cross-check against the three premises

### 1. "Enforcing an `[Authorize]` attribute would be complicated" — not the case

This is a one-line change, and both spellings are documented and first-class:

```csharp
endpoints.MapGet(CatalogRoutes.Catalogue, [Authorize(Policy = CatalogApiPolicy.Name)] Search);
```

`[AllowAnonymous]` and `[Authorize(AuthenticationSchemes = …)]` work on minimal API endpoints too. So
the attribute vocabulary is **not** a reason to change endpoint style.

### 2. "The framework already provides the pipeline we hand-rolled" — yes, and .NET 10 closed the gap

- Automatic **400 on a bad query value**: already true in minimal APIs (binding failure table).
- Automatic **validation → 400 with the errors**: added to minimal APIs in .NET 10 via `AddValidation()`,
  with `IProblemDetailsService` for shape. Controllers get the equivalent through `[ApiController]`'s
  model-state check. Both are real; neither is exclusive to controllers any more.

### 3. "Controllers are the best-practice endpoint implementation" — not as a general rule

Microsoft's stated default for new projects is minimal APIs, and the most-starred **actively maintained**
template uses them; the controller-shaped references are both archived. Controllers are the right call
when the features in the table above are needed. Two of those — binding extensibility and advanced
validation — become relevant here, and the next section shows why: only to reproduce behaviour we
already have.

**Where controllers genuinely win for this application** (not in the docs table, but real):

- **A class-level `[Authorize]` gates every action by default.** In a `MapEndpoints` file, a new route is
  ungated unless its author remembers `.RequireAuthorization` — a silent hole. This is the strongest
  argument for the change, and it grows with the surface. (Minimal APIs can match it with a global
  `FallbackPolicy`, which is the mitigation if we stay.)
- **Discovery**: `MapControllers()` finds controllers; a `MapEndpoints` file grows a list that must be
  maintained by hand.
- Filters (`IAsyncActionFilter`) attach cross-cutting behaviour by attribute instead of by inline call.

## What is wrong with the code today

Measured, from the source:

| # | Finding |
|---|---|
| **R1** | `CatalogController` and `AccountController` **are not controllers**; they are static minimal-API classes whose own remarks argue against MVC. The name states something untrue, and it is what a reader sees first. |
| **R2** | `CatalogApiParameters` + `CatalogApiBinding` (≈90 lines) hand-roll query binding, case-insensitive enum matching and the refusal message — work the framework now offers (`AddValidation()`, or `[ApiController]` model state). |
| **R3** | The **400 body is a bare string** (`"Unknown category 'sofa'. Use one of: …"`). Every other error shape in modern ASP.NET Core is RFC 9457 `ProblemDetails`, which `[ApiController]` produces by construction and `AddValidation()` produces in minimal APIs. An agent parsing our 400 has to special-case it. This is the finding I would act on regardless of endpoint style. |
| **R4** | `CatalogApiLog.Called(...)` is invoked inside the handler. A filter makes it declarative and impossible to forget on a second endpoint. |
| **R5** | The gate is attached from outside by convention (`.RequireAuthorization`), not declared on the type — see the class-level argument above. |

**Not findings:** `[ProducesResponseType]` vs `.Produces<T>()` is a spelling difference with identical
metadata; the same goes for `[HttpGet]`+`[Route]` versus `MapGet`. Nothing is gained by changing those
alone.

## The part that is not free: our 400 contract

Measured, because it decides whether "switch to controllers" actually deletes the hand-rolled code:

- `[ApiController]` binding an invalid enum gives model state → 400 ProblemDetails whose
  `errors["category"]` reads *"The value 'sofa' is not valid."*
- `[AllowedValues("chair", "desk", …)]` gives *"The category field does not equal any of the values
  specified in AllowedValuesAttribute."*

Neither **names the allowed values**, and `API-01` requires exactly that: the body must contain
`chair`, `desk` and `accessory`. So the refusal behaviour survives the change only by custom code —
a custom `IModelBinder`/`ProblemDetailsFactory` under MVC, or a custom `ValidationAttribute`/endpoint
filter under minimal APIs. **The hand-rolled binder is the price of a deliberate, tested contract, not
evidence of the wrong endpoint style.**

The landing spot I would recommend either way: keep "the refusal names the values", but deliver it as
`ProblemDetails` (values listed in the `errors` entry), so the body is standard *and* machine-usable.
`API-01` then asserts both the shape and the values, which is a stronger test than today's substring
check.

## Two ways to align

### Option A — real controllers

```csharp
[ApiController]
[Route("api/catalog")]
public sealed class CatalogController(...) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = CatalogApiPolicy.Name)]
    [ProducesResponseType<IReadOnlyList<ProductView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyList<ProductView>> Search(
        [FromQuery] CatalogCategory? category,
        [FromQuery] CatalogSubCategory? subCategory,
        [FromQuery] string? search) => …;
}
```

Wiring: `builder.Services.AddControllers()` and `app.MapControllers()`.

**The catch to plan for:** `ConfigureHttpJsonOptions` — where the camelCase + `JsonStringEnumConverter`
configuration lives — applies to minimal APIs and `Results`, **not** to controllers. Under MVC the same
configuration must move to `AddControllers().AddJsonOptions(...)`, or `API-07` breaks immediately
(enums serialize as numbers). One configuration, one place, moved deliberately.

Costs: MVC's pipeline enters an application built without it; the 400 contract needs the custom binder
described above (or the contract changes); the OpenAPI transformer, which currently looks up the path
`/api/catalog` in the document, must be re-pointed (controllers expose `RelativePath` without the
leading slash) or moved to metadata-based detection; `CatalogRoutes` loses its single-source role for
the path, since `[Route]` states it.

Buys: class-level gating, discovery, filters, conventions, `[ApiController]` invariants.

### Option B — keep minimal APIs, adopt the framework

- `builder.Services.AddValidation()`; the filter becomes a validated record; `CatalogApiParameters` and
  `CatalogApiBinding` are deleted.
- `[Authorize(Policy = …)]` directly on the handler (or keep `.RequireAuthorization`).
- `AddProblemDetails()` + `UseStatusCodePages()`, and the refusal delivered as `ProblemDetails`.
- Rename `CatalogController`/`AccountController` to `CatalogEndpoints`/`AccountEndpoints` (or adopt the
  20.5k-star `IEndpointGroup` shape), so no name lies.
- Logging becomes an `IEndpointFilter` (or stays inline).
- Optionally a global `FallbackPolicy` to close R5.

Costs: none structural; the API keeps Microsoft's recommended and best-supported path.

### What does not change

The gate's meaning (the configured claim), the policy names, the bearer/cookie split, JSON shape
(other than the error body), the document, the dev-only UI, and the test matrix entries (`API-01…API-16`)
except where the 400 body is asserted.

## Recommendation

1. **Fix R1 and R3 regardless of style.** The misleading name costs nothing to fix, and `ProblemDetails`
   is the one genuine best-practice gap in the current code.
2. **Then choose on one question only:** is this application's HTTP surface going to grow past the
   catalogue and the two account redirects?
   - **Yes, or you want the classic pipeline for consistency:** take **Option A** now — it is cheapest
     while there is one endpoint, and it buys the class-level gate before there are routes to forget.
   - **No:** take **Option B**; it keeps Microsoft's recommended path and deletes the same hand-rolled
     code, and R5 is closed with a fallback policy rather than a rewrite.
3. **Do not expect the switch itself to delete custom code.** R2's binder is replaced, not removed,
   unless we accept the default 400 message.

## If approved

One epic, `e02-api-surface`, three stories, each a revertible commit with its own verification:

| Story | Work | Tests |
|---|---|---|
| `e02s01` | `ProblemDetails` for the refusal; the values named in `errors`. No structural change. | `API-01` reasserted on shape **and** values; problem-details tests |
| `e02s02` | The endpoint style change (A or B), including the JSON configuration move if A, the transformer re-point, and the rename. | `API-01…API-16`; `CatalogApiParametersTests` replaced by binding tests; new test that the route is gated *without* relying on a convention |
| `e02s03` | Logging as a filter; `AccountController` renamed/converted; `README` and `specs/test-matrix.md` updated | `CatalogApiLoggingTests`; the funnel E2E |

Baselines that must hold at every step: BUILD clean; NONBROWSER **539**; BROWSER **104 passed,
1 skipped**. Non-vacuity proof required for any new architecture test. Security review addendum needed
in `specs/security/REVIEW.md` if the authentication path's wiring moves (it should not).

## Two questions before any code is written

1. **Option A or Option B?** (the growth question above decides it)
2. **`ProblemDetails` now, or keep the plain-string 400?** My recommendation is now — a plain string is
   the only part of this API that does not match current practice.

## Note on "remove DDD"

Read as: keep DDD / Clean-Architecture framing out of this recommendation, and base it on ASP.NET Core
practice — which is what the table above does. If it meant removing DDD concepts from the codebase,
that is a much larger, separate change and needs its own scoping conversation.
