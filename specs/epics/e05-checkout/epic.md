# E5 — Checkout: the demo gate, the order, the reset

**Slice:** 1 · **Status:** ready · **ADR:** 0006, 0007, 0010

## Goal
"Rent This Setup" turns a draft into an order — gated by a typed confirmation,
idempotent, and followed by a cleared cart and a confirmation view.

## In scope
- `DemoConfirmDialog`: native `<dialog>` + `showModal()`, styled from `DESIGN.md`
  Level-2 tokens, stating that no money changes hands.
- Phrase gate: trimmed and **case-insensitive**, otherwise exact. Confirm control
  disabled until it matches. **Re-validated on the server** so the order cannot
  be created by posting around the dialog. Cancel closes and leaves the cart
  untouched.
- `CheckoutCommand` orchestration: validate gate → validate address → obtain the
  **frozen composition** from Workspace → price via Catalog → create `Rental`
  with snapshotted lines and the delivery fee → raise and immediately settle the
  first `Invoice` → transition the draft `Draft → Converted` → return the order
  and its access token.
- **Idempotency:** the `Draft → Converted` state is the key. A repeat checkout
  returns the existing order rather than creating a second.
- Guards: empty workspace and a missing or too-short address are both refused
  server-side; an unresolvable SKU reference returns a validation error, never a
  500.
- Cart reset: draft cleared and the cookie token rotated (forced full-page
  navigation so the middleware can act).
- Order confirmation view at `/orders/{number}`, reachable only with the access
  token: order number, lines with quantities, subtotal, one-time fee, grand
  total, delivery address.

## Out of scope
Scheduling, activation and renewals (E6).

## Stories
1. As a customer I want to be told clearly that this is a demonstration.
2. As a customer I want to confirm deliberately by typing the phrase.
3. As a customer I want a mistyped phrase to be caught, not swallowed.
4. As a customer who double-clicks I want exactly one order.
5. As a customer I want proof of my order and a link I can come back to.
6. As a customer I want the builder emptied afterwards, ready for the next setup.
7. As an attacker I want to fail: no phrasing, no tampering and no direct POST
   may create an order or alter an amount.

## Definition of done
Build clean; unit + integration tests green; the funnel works end to end in a
real browser; matrix rows passing; archived.

## Matrix rows
CO-01…CO-15 · ORD-01…ORD-05 · DR-01 · NAV-01 · NAV-06 · SEC-01 · SEC-03

## Risks
- The token rotation depends on a real HTTP round trip; without the forced
  navigation the reset silently does not happen. This is called out in the E2E
  suite as an explicit assertion.
