# ADR-0023: The UI reaches the application through interfaces

- **Status:** Accepted (2026-09-13)

## Context
The layer graph was already sound: Domain depends on nothing but `BuildingBlocks.Domain`,
Application on Domain and on other modules' Application **contracts**, Infrastructure on Domain, and
the Host is the composition root. Architecture tests hold that line (ARC-01, ARC-04), and nothing in
Domain or Application constructs a service.

What remained coupled was the last seam. A Razor component injected the **concrete handler class**
that happened to answer its question — `GetCatalogPageHandler` into four components,
`GetWorkspaceHandler` and five siblings into the session, `GetRentalByTokenHandler` into the order
page. The effect was that a component knew how the application is organised, a handler could not be
renamed or split without editing markup, and no cross-cutting concern (logging, validation) could be
wrapped around a handler because there was no interface to wrap.

## Decision
- **A type that crosses a layer boundary gets an interface.** Ten application handlers that the UI or
  the session reaches — two in Catalog, two in Rentals, six in Workspace — now have one, named after
  the operation rather than the mechanism (`IGetCatalogPage`, `IStartDraft`), and registered beside
  their implementation.
- **The UI depends on `IWorkspaceSession`, not on the session.** Components and the workspace
  observer base take the interface; the session is registered under it in the **same lifetime**, so
  the circuit's state behaves exactly as it did.
- **DI provides the browsing state.** The catalog browser is registered as a transient whose
  page-query delegate is resolved from `IGetCatalogPage`, so a component injects it instead of
  constructing it — and each component still gets its own, which is what keeps the panel's category
  and the store's from becoming one.
- **An architecture rule holds the seam open.** A component may not name a type ending in `Handler`;
  the rule asserts the handlers still exist so it cannot pass by finding nothing.

## Consequences
- **Interfaces are for boundaries, not for everything.** Domain policies (`RenewalPolicy`,
  `DeliveryPolicy`, `CancellationPolicy`, `SlotRules`), presentation helpers (`ProductGlyph`,
  `SlotCss`, `AccessoryGroups`, `CheckoutGate`) and settings records keep their shape: they cross no
  layer, so an interface in front of them would be ceremony rather than a seam.
- **Lifetimes are part of the decision.** Handlers and the session stay scoped; the browser stays
  transient. Changing one in a Blazor circuit is a behaviour change, so the interfaces are registered
  in the existing lifetime deliberately rather than by default.
- **Cross-cutting concerns are now wrappable.** A decorator over a handler interface is possible. None
  is added: without a requirement it would be speculation, and the plan stops at enabling it.
- **No mediator.** A dispatcher would replace one indirection with another and hide which handler a
  component needs; a named interface says it out loud, which is what an AI-navigable codebase wants.

## Alternatives rejected
- **An interface in front of every public type.** It would multiply the surface without adding a seam,
  and the boundary rule above is easier to keep honest than a blanket convention.
- **A single facade per module** (`ICatalogQueries`, `IWorkspaceCommands`) instead of one interface per
  operation. Coarser, and it would grow a method for every new query, becoming the god-object the
  handlers exist to avoid.
- **A mediator or request dispatcher.** It trades a compile-time dependency for a runtime string, which
  is the opposite of the goal.
- **Leaving the session concrete.** It is the circuit's state hub and the one type most worth being
  able to give a component without a database behind it.
