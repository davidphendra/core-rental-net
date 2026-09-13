# Refactor plan — deepen the ten friction points

## Problem Statement

The `src` tree has 180 files, 60 of which are over 50 lines, and the highest-churn files cluster
around the builder/store browsing surfaces and the composition root. Line count is a symptom, not
the problem. The real problems, found with the `deepen-architecture` method (Module Depth score +
deletion test) and a churn ranking, are:

1. **Duplicated behaviour, not duplicated text.** The category-browsing state machine is written
   twice (panel and store); the image/placeholder decision is written in five places; the session's
   refusal policy is written twice and the two copies disagree.
2. **Behaviour buried in long procedures.** Checkout's seven ordered steps and the scheduler's four
   lifecycle rules live inline, so each rule is only readable in the middle of a method.
3. **One class, several jobs.** The catalog loader mixes I/O, parsing, validation and image policy;
   the test host mixes four hosts, process management and readiness; the identity registration mixes
   six concerns.
4. **One name that lies.** `TokenEventHandlerMiddleware` owns the *draft* cookie, not identity —
   the ambiguity that already caused a real misdiagnosis during the Auth0 work.

The goal is **deeper modules**, not smaller files: a smaller interface hiding more behaviour
(Ousterhout). Every proposed abstraction must pass the deletion test — delete it and the complexity
reappears at several callers.

## Solution

Ten refactors, applied safest-first in three phases. Each phase keeps the whole suite green, and
each commit is individually revertible.

Patterns are used only where they have a forcing function. Where a pattern would duplicate behaviour
the domain already owns (a State pattern over the rental transitions), it is **rejected** and
recorded below.

| # | Finding | Pattern(s) applied | Pattern(s) rejected |
|---|---|---|---|
| C8 | Middleware misnamed | — (naming) | — |
| C10 | Duplicate catch blocks | Guard / exception filter | — |
| C4 | Session refusal policy written twice | Template Method; the class stays a Facade | Result monad (system-wide change) |
| C1 | Image/placeholder decision in 5 places | Extract Component; Parameter Object | boolean "bare mode" flag |
| C2 | Browsing state machine duplicated | State Container / Presentation Model; Observer | generic render-fragment component |
| C9 | Composition root does six jobs | Module pattern; Builder (existing extensions) | DI container refactor |
| C3 | Checkout is a numbered procedure | Extract Method; Specification for preconditions | Chain of Responsibility (over-abstraction at three rules) |
| C6 | Scheduler transitions inline | Extract Method; Specification + Command per transition | State pattern (would duplicate the aggregate's own transitions) |
| C5 | Loader mixes four jobs | Ports & Adapters; Data Mapper; Chain of Responsibility (image) | Repository rewrite |
| C7 | Test host does everything | Factory; Object Mother; Test Data Builder | — |

**Invariant each phase must preserve (the hard gate):**

- **C1** — no image file ⇒ glyph placeholder, never a broken image.
- **C2** — unknown or remembered category falls back to **Desks**; search belongs to the tab and is
  cleared when the tab changes.
- **C3** — nothing is refused after the draft becomes terminal; a second attempt returns the **same**
  order (idempotency).
- **C4** — the session is a cache, never a source of truth; a refusal is a message, not a crash.
- **C5** — the running app never depends on a third-party image host; every load failure names the
  file and, where relevant, the SKU.
- **C6** — a second run on the same date is a no-op; a run after a gap catches up.
- **C7** — the default suite stays hermetic; the real-tenant run stays opt-in.
- **C8** — no behaviour change at all; rename only.

## Commits

Shorthand used by the verify steps:

- `BUILD` → `dotnet build CoreRentalNet.sln`
- `NONBROWSER` → `dotnet test CoreRentalNet.sln --filter "FullyQualifiedName!~CoreRentalNet.E2E"`
- `BROWSER` → `dotnet test tests/CoreRentalNet.E2E`

### Phase 0 — the safety net

```
0. Record the baseline: NONBROWSER green (425) and BROWSER green (99 passed, 1 opt-in skipped) → verify: NONBROWSER && BROWSER
```

No code changes. If the baseline is not green, stop and fix that first.

### Phase 1 — structure only, no behaviour change

```
1. Rename TokenEventHandlerMiddleware to DraftTokenMiddleware, its RotateQueryKey/ItemsKey members
   unchanged, and update its three call sites (pipeline registration, the root component's token
   resolution, the review page's rotate query). No logic touched. → verify: BUILD && BROWSER

2. Collapse the two identical catches in the review page's confirm handler into one exception filter
   (DomainRuleViolationException or NotFoundException). → verify: BUILD && BROWSER

3. Extract one private RunAsync skeleton in the session (operation + on-refusal callback) and route
   both MutateAsync and the try-set-address path through it, so the refusal policy exists once. Keep
   the two public shapes identical. → verify: NONBROWSER && BROWSER

4. Extract the inner "image or glyph" decision into a wrapper-less visual component, and have the
   existing ProductImage delegate to it. No caller changes yet. → verify: BUILD && BROWSER

5. Use the wrapper-less visual in the store card and the builder card, keeping each card's own
   wrapper, badge and hover. → verify: BUILD && BROWSER

6. (optional) Introduce a small product-media value object so the visual component takes one
   parameter instead of four, and update its callers. → verify: BUILD && BROWSER
```

### Phase 2 — structural extraction

```
7. Add the CatalogBrowser state container (tab, query, items, empty message, change notification)
   with unit tests for its rules: fallback to Desks, search cleared on tab change, empty message.  → verify: NONBROWSER

8. Move the store page onto the state container and delete its duplicated fields. → verify: BUILD && BROWSER

9. Move the selection panel onto the same state container and delete its duplicated fields. → verify: BUILD && BROWSER

10. Split the identity registration's configure delegate into named concerns (Auth0 wiring, cookie
    policy, OpenID Connect overrides, access-token claim lifting, token-handler registration),
    inline only. No option changes. → verify: NONBROWSER && BROWSER

11. Extract checkout's ordered steps into named private methods (satisfy the demo gate, require
    something to order, require an address, price the lines, convert the draft, place the order),
    keeping the existing order and the existing exceptions. → verify: NONBROWSER

12. (optional) If, and only if, the preconditions grow past four, introduce an ICheckoutRule
    specification list and have the service iterate it. → verify: NONBROWSER

13. Extract the scheduler's four lifecycle transitions into named private steps called in the same
    order, preserving catch-up semantics. → verify: NONBROWSER
```

### Phase 3 — deeper seams

```
14. Extract a pure record mapper from the catalog loader (sku, category, subcategory, badge, money,
    description validation) with no filesystem access, and cover it with unit tests built from
    in-memory records. → verify: NONBROWSER

15. Introduce an ICatalogSource port with a file adapter and an in-memory adapter; the loader
    becomes read → map → duplicate-check. The in-memory adapter is the second adapter that makes the
    seam real, and the integration test that reads the real file keeps the file adapter honest. → verify: NONBROWSER

16. Extract image resolution into a small resolver with a chain (vendored file → remote assumption →
    missing) so the availability rule lives in one place. → verify: NONBROWSER

17. Split the test host into a process adapter plus one factory per host (guest, authenticated,
    local-provider-backed, real-tenant), leaving the existing fixture as the composition that starts
    only what a test needs. → verify: BROWSER
```

## Decision Document

**Modules built or modified**

- `DraftTokenMiddleware` (renamed from `TokenEventHandlerMiddleware`) — the draft cookie's lifetime.
- A **product visual** component, wrapper-less, plus the existing wrapper component delegating to it.
- **CatalogBrowser** — a presentation state container for "which category and search am I showing".
- **Identity registration concerns** — named private configuration steps behind the existing
  extension methods.
- **Checkout steps** and **scheduler transitions** — named private methods; no new public surface.
- **Catalog record mapper**, **ICatalogSource** port, **image resolver** — the three parts of the
  loader, with the port as the new seam.
- **Test host factories** — one per host, over the existing process adapter.

**Interfaces changed**

- The product visual component's parameter list (toward a single media value).
- `ProductImage` keeps its current parameters and delegates; no caller of it changes.
- `ICatalogSource` is new and is the only new public interface in the plan.
- Everything else is internal restructuring: the same types, the same public methods, the same
  exceptions.

**Technical clarifications / architectural decisions**

- The exception-based refusal policy stays. Expected refusals remain `DomainRuleViolationException`
  and `NotFoundException`, caught at the presentation seam. A Result type was considered and
  **rejected** as a system-wide change disguised as tidying.
- The session stays a **cache**, never a source of truth: every read still goes through a handler.
- The catalog port is justified by **two adapters** (file and in-memory); one adapter alone would be
  a hypothetical seam and the port would not earn its keep.
- No State pattern over rental statuses: the aggregate already owns its transitions, and a State
  object would duplicate them.
- Chain of Responsibility is used only for image resolution, where the alternatives are genuinely
  ordered and each can decline.

## Testing Decisions

**What makes a good test here:** assert observable behaviour through a module's interface, never its
internals. The suite already does this — the browser tests wait for a marker element and read the
page, the unit tests call handlers and read views. The refactor must not add tests that reach into a
private method it just created.

**Coverage that already protects this work**

- Browsing and the store: `StoreTests`, `CatalogBrowsingTests`, `NavigationTests`.
- Checkout and idempotency: `CheckoutTests`, `PlaceOrderTests`, `EndToEndCheckoutTests`.
- Scheduling: `SchedulingTests`, `RenewalSchedulingTests`, `RentalLifecycleTests`.
- Catalog loading: `CatalogFileTests`, `CatalogQueryTests`.
- Session behaviour: `WorkspaceCommandTests`, `WorkspacePersistenceTests`.
- Architecture rules that must stay green: `SourceLayoutTests`, `CommittedConfigurationTests`,
  `DesignTokenTests`, `AuthorizationGateTests`, `AccountSurfaceTests`.

**New tests proposed**

- The **CatalogBrowser** container: unit tests for fallback, search-cleared-on-tab-change, and the
  empty message. This is the one genuinely new behaviour surface, so it gets its own tests.
- The **catalog record mapper**: table-style unit tests for each rejection (invalid SKU, unknown
  category, unknown subcategory, unknown badge, missing image, missing name) and each acceptance.
- The **image resolver**: vendored file wins, remote assumed available, local-missing reported
  unavailable.

**How this is verified**

- Every commit: `BUILD`, then `NONBROWSER` for domain/application changes, `BROWSER` for
  presentation changes, and both when a commit spans them.
- The refactor changes no matrix row's expectation; `specs/test-matrix.md` should need no edits. If a
  row's expectation has to change, the refactor has changed behaviour and must be stopped.

## Out of Scope

- The deep domain modules — `Workspace`, `Rental`, `Invoice`, `Money`, `Product`. Their length is
  leverage; splitting them would trade locality for line count.
- The exception-versus-result policy.
- The `RoleClaims` permission-suffix convention (already flagged as fragile in ADR-0022).
- Any change to authentication, authorization or token handling beyond the rename in commit 1.
- New third-party packages. No new dependency is needed for any commit here.
- Renaming other types, reformatting, or comment rewrites beyond what a commit needs to read well.

## Further Notes

- Refactoring vocabulary follows Fowler's catalog; the structure-before-behaviour ordering follows
  Kent Beck's *Tidy First*. Structure commits and behaviour commits are not mixed.
- The churn ranking put the selection panel, the builder, the store and the review page at the top.
  Commits 4–9 are aimed exactly there, so the highest-churn files end up with the least duplicated
  logic.
- If a commit cannot be made to leave the suite green, split it further rather than proceeding. The
  plan is a sequence of tiny, individually revertible steps, not a batch.
- Suggested next step after this plan: create a dedicated branch before the first commit.
