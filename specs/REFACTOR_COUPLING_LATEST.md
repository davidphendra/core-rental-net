# Refactor plan — loose coupling by interface between the layers

## Problem Statement

The layer graph is already sound — Domain depends on nothing but `BuildingBlocks.Domain`,
Application depends on Domain and on other modules' Application **contracts**, Infrastructure
depends on Domain, and Host is the composition root. Architecture tests already hold that line
(ARC-01, ARC-04). There is no concrete `new` of a service anywhere in Domain or Application.

The coupling that remains is at exactly one seam: **the UI reaches into the Application layer by its
concrete handler classes.**

- Six distinct Application handlers are injected directly into Razor components — across eight files.
- `WorkspaceSession` (the circuit's state hub) takes **six more concrete handlers** in its constructor.
- Components construct their own `CatalogBrowser`, calling a static helper over the concrete handler.
- Components inject the concrete `WorkspaceSession` rather than an abstraction.

So the UI cannot be exercised without the real handlers, a handler cannot be renamed or split without
touching markup, and no cross-cutting decorator (logging, validation, caching) can wrap a handler,
because there is no interface to wrap. This plan introduces interfaces at that seam — and only there.

## Solution

Introduce an interface for each Application handler that crosses a layer boundary, register it beside
its implementation, and move every consumer onto the interface. Then expose the session itself as an
interface, let DI provide the catalog browser, and add an architecture test so the seam cannot silently
re-open.

**Rule for what gets an interface:** a type that crosses a layer boundary — UI/Composition reaching
into Application, or one module reaching into another. Types that do not cross a boundary keep the
shape they have. This is deliberately narrower than "put an interface in front of everything", because
an interface with one implementation and no test double is a hypothetical seam, not a real one.

Interfaces are introduced **additively**: the interface and its registration appear first, consumers
move one at a time, and the concrete registration is removed only after the last consumer has moved.
Every commit leaves the whole suite green.

## Commits

Shorthand:

- `BUILD` → `dotnet build CoreRentalNet.sln`
- `NONBROWSER` → `dotnet test CoreRentalNet.sln --filter "FullyQualifiedName!~CoreRentalNet.E2E"`
- `BROWSER` → `dotnet test tests/CoreRentalNet.E2E`

### Phase 0 — the safety net

```
0. Record the baseline: NONBROWSER green and BROWSER green → verify: NONBROWSER && BROWSER
```

### Phase 1 — interfaces for the handlers the seam uses

Each module in turn. Within a module the interface is added and registered first, consumers are moved
one file at a time, and the concrete registration is retired last.

**Catalog**

```
1.  Add IGetCatalogPage and IGetFeaturedProducts; the existing handlers implement them; register each
    interface beside its implementation, in the same lifetime. → verify: BUILD && NONBROWSER

2.  Make the tab loader take IGetCatalogPage instead of the concrete handler. → verify: NONBROWSER

3.  Move the selection panel, the slot picker dialog, the store page and the builder page onto
    IGetCatalogPage. One file per commit; the suite must stay green after each. → verify: BROWSER

4.  Move the home page onto IGetFeaturedProducts. → verify: BROWSER

5.  Remove the concrete handler registrations that nothing resolves directly any more. → verify: BROWSER
```

**Rentals**

```
6.  Add IGetRentalByToken and IGetInvoicesByToken; register each beside its implementation. → verify: NONBROWSER

7.  Move the order confirmation page onto both interfaces. → verify: BROWSER

8.  Remove the concrete registrations. → verify: BROWSER
```

**Workspace**

```
9.  Add an interface for each of the six handlers the session uses, and register each beside its
    implementation. → verify: NONBROWSER

10. Change the session's constructor to take the six interfaces. → verify: NONBROWSER

11. Remove the concrete registrations the session was the last consumer of. → verify: BROWSER
```

### Phase 2 — the session itself behind an interface

```
12. Add IWorkspaceSession with the session's existing public surface; WorkspaceSession implements it;
    register the interface in the same lifetime as the concrete type. → verify: NONBROWSER

13. Move the workspace observer base and the three components that inject the session onto the
    interface. → verify: BROWSER

14. Remove the concrete registration; the composition now names the interface only. → verify: BROWSER
```

### Phase 3 — let DI provide the catalog browser

```
15. Register the catalog browser as a transient whose page-query delegate is resolved from
    IGetCatalogPage, and have the two components inject it instead of constructing it. → verify: BROWSER
```

### Phase 4 — lock the seam and record the decision

```
16. Add an architecture test: a Host component must not depend on a type whose name ends in Handler;
    it must name the interface. Prove the rule is not vacuous by asserting the handlers under test
    actually exist. → verify: NONBROWSER

17. Add an ADR recording that the UI reaches the application only through interfaces, and update the
    tech-architecture note with the seam. → verify: BUILD
```

## Decision Document

**Modules built or modified**

- **Application handler interfaces** — one per handler that crosses a boundary: two in Catalog, two in
  Rentals, six in Workspace. Each names the operation, not the mechanism (the operation *is* the
  abstraction), and carries the method the handler already exposes.
- **`IWorkspaceSession`** — the presentation seam the components depend on, so a component can be
  given a session without one existing.
- **`CatalogBrowser` registration** — the delegate it already takes, resolved from DI, so components
  stop constructing it.
- **The composition registrations** — each module registers its interfaces beside its implementations
  during the change, then drops the concrete registrations once nothing resolves them.

**Interfaces changed**

- The tab loader's parameter becomes the Catalog query interface.
- The session's constructor takes six interfaces instead of six classes.
- Component injections name interfaces.
- No public method signature on a handler changes; no route, no view model, no database shape changes.

**Architectural decisions**

- **Interfaces are introduced at boundaries only.** Domain policies, presentation helpers and settings
  records keep their current shape; they do not cross a layer and an interface would be ceremony.
- **The catalog loader keeps its static entry point.** It runs once at composition time behind a
  module port (`IProductCatalog`) that already exists. A second interface in front of it would not
  change how it is reached. This is the same conclusion the previous refactor reached and it stands.
- **Lifetimes do not change.** Handlers and the session stay scoped; the browser is transient, which
  is what gives each component its own browsing state. A lifetime change here would be a behaviour
  change in a circuit, so the interfaces are registered in the existing lifetime deliberately.
- **No decorator is added now.** The interfaces make one possible; adding one without a requirement
  would be speculative. The plan stops at enabling it.
- **No mediator or dispatcher is introduced.** A mediator would replace one indirection with another
  and hide which handler a component needs. Named interfaces say it out loud.

## Testing Decisions

**What makes a good test here:** the seam is a type-level fact, so the tests are of two kinds — an
architecture rule that reads the dependency, and the existing behavioural suites that prove the wiring
still works. No test should assert that a specific class is registered.

**Coverage that protects this work**

- The **browser suite** is the safety net for Phase 1 and 3: every moved component is rendered and
  exercised through a real process, so a mis-resolved interface fails at start-up or on the page.
- The **host unit tests** cover the session's refusal behaviour and the browser's rules; they run
  unchanged, which is the point — the session's observable behaviour must not move.
- **Module boundary tests** (ARC-01, ARC-04) already forbid a module reaching another's Domain or
  Infrastructure; nothing here relaxes them.

**New tests proposed**

- One architecture rule: a Host component must not name an Application handler type directly. It is a
  source-level check (the same style as the existing claim-value and clock rules), because the
  Component layer and the composition root live in the same assembly and NetArchTest cannot tell them
  apart by namespace alone.
- The rule must assert that handlers exist in the assembly it scans, so a rename cannot make it pass
  vacuously.

**Verification**

- Every commit: `BUILD`, then the suite the layer touches, and both when it spans them.
- No matrix row's expectation changes. If one has to, behaviour moved and the commit is wrong.

## Out of Scope

- Replacing the exception-based refusal policy, or introducing a Result type.
- A mediator, dispatcher, or request-pipeline abstraction.
- Interfaces for Domain policies (`RenewalPolicy`, `DeliveryPolicy`, `CancellationPolicy`,
  `SlotRules`), which are pure and cross no boundary.
- Interfaces for presentation helpers (`ProductGlyph`, `SlotCss`, `AccessoryGroups`, `CheckoutGate`,
  the tab mapping) and for settings records.
- Any change to persistence, EF Core configuration, migrations, routes or view models.
- Authentication and authorization, beyond their handler already implementing the framework's own
  interface.

## Further Notes

- The change is mechanical and low-risk by construction: an interface is added before anything moves,
  consumers move one at a time, and the concrete registration is retired only when nothing resolves
  it. If a commit cannot be made green, it is split further rather than forced.
- The value is threefold: the UI stops depending on how a handler is written; the session becomes
  testable without a database; and cross-cutting concerns become wrappable at a real seam.
- The risk that matters is **ceremony creep** — an interface for everything. The boundary rule in the
  Decision Document is the guard, and the architecture test in Phase 4 is what keeps it honest.
