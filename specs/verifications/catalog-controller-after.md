# The catalogue endpoint as a controller — what was run and observed

Implements Option A of `specs/WEB_API_ALIGNMENT_LATEST.md`. Branch `catalog-controller`, one commit.

## What changed

| File | Change |
|---|---|
| `Controllers/CatalogController.cs` | Was a `static class` mapping a route with `MapGet`. Is now `[ApiController] [Route(CatalogRoutes.Catalogue)] class CatalogController : ControllerBase` with `[HttpGet]`, `[Authorize(Policy = CatalogApiPolicy.Name)]`, `[ProducesResponseType]`, and framework-bound `[FromQuery]` parameters. |
| `Controllers/CatalogEnumBinder.cs` | **New.** A model binder for one catalogue filter: a word the catalogue publishes binds; anything else becomes a model-state error. |
| `Controllers/CatalogEnumBinderProvider.cs` | **New.** Supplies that binder for `CatalogCategory` and `CatalogSubCategory`, named explicitly rather than "every enum". |
| `Controllers/CatalogApiParameters.cs` | Now the vocabulary alone (`TryMatch`, `Names`, `Refusal`). The `Bind(...)` entry point and its result record are gone. |
| `Controllers/CatalogApiBinding.cs` | **Deleted.** The hand-checked "query or error" result is what `[ApiController]`'s model state replaces. |
| `Composition/CatalogApiRegistration.cs` | Registers the controller pipeline and the binder provider, and applies the one JSON configuration to **both** controllers and endpoint mappings. |
| `Composition/ApplicationPipeline.cs` | `CatalogController.MapEndpoints(app)` → `app.MapControllers()`. |

Unchanged, deliberately: the policy and its name, the bearer/cookie split, the route, the JSON shape,
the log line, and the security-scheme transformer — which still finds the operation because
`CatalogRoutes.Catalogue` remains the document's path key.

## The contract, measured before and after

The refusal was a **bare string** body. `[ApiController]` answers a model-state error with the
framework's own error shape, so the shape standardised while the behaviour stayed:

| | Before | After (measured) |
|---|---|---|
| Content type | `application/json` | **`application/problem+json`** |
| Body | `Unknown category 'sofa'. Use one of: chair, desk, accessory.` | `{"status":400, …, "errors":{"category":["Unknown category 'sofa'. Use one of: chair, desk, accessory."]}}` |
| `API-01` assertion | body contains *sofa, chair, desk, accessory* | unchanged, and now asserted against `errors.category[0]` and the content type |

## Two things the plan predicted, both of which turned out to be real

**1. The switch does not delete the custom code — it moves it.** With the binder provider removed from
the registration, both refusal tests fail, and the measured default is:

> `Expected refusal "The value 'sofa' is not valid." to contain "chair".`

That is MVC's own message, and it does not name the allowed values. So the hand-rolled binder was the
price of a deliberate, tested contract, exactly as `WEB_API_ALIGNMENT_LATEST.md` said. What improved is
not that custom code vanished, but that it now produces a **model-state error** instead of a
hand-checked result: the action has no failure branch to write and none to forget, and the framework
owns the status code, the content type and the body.

**2. `ConfigureHttpJsonOptions` does not apply to controllers.** Removing `.AddJsonOptions(...)` from
the registration fails `API-07` with:

> `The requested operation requires an element of type 'String', but the target element has type 'Number'.`

i.e. the controller wrote `"category": 2` where the contract is `"category": "desk"`. The one
configuration is now applied to both serializer option sets from a single private method.

Both experiments were run by temporarily editing the registration and restoring it; each was restored
and the full suite re-run green afterwards.

## Baselines

| Baseline | Before | After |
|---|---|---|
| BUILD | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| NONBROWSER | 539 | **542** (+3: two refusal-shape tests, and the vocabulary tests split) |
| BROWSER | 104 passed, 1 skipped | **104 passed, 1 skipped** |

`API-01` … `API-16` all still hold, including `API-13`/`API-14` (the document still describes
`/api/catalog` with parameters `category`, `subCategory`, `search`, responses `200` and `400`, and the
enum vocabulary as strings) and `API-08`/`API-09`/`API-10` (401 without a token, 403 with the wrong
permission, 200 with the configured one — now enforced by the `[Authorize]` attribute).

## Deliberately not in this change

- **`AccountController` is still a static endpoint class**, and its name says otherwise. It is not a
  Web API endpoint: it is two OIDC redirects that a Blazor circuit cannot perform. Converting it
  touches the sign-in flow, so it is a separate, smaller change with its own verification rather than
  a rider on this one.
- **The request log is still written in the action**, not by a filter. A filter would be declarative,
  but the count it records is a property of the action's result; reading it out of an
  `ObjectResult` inside a result filter would be more machinery than the one line it replaces.

## What this change buys, honestly

`[Authorize]` on the action rather than `.RequireAuthorization` from outside, MVC's binding and
validation instead of a hand-checked result, attributes for the documented answers, and discovery by
`MapControllers()` — so a second controller need not be remembered in the pipeline. It does not make
the refusal contract cheaper, and it does not remove the vocabulary type that names the words.
