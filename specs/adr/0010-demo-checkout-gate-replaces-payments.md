# ADR-0010: The demo confirmation gate replaces payment integration

- **Status:** Accepted (2026-09-10)
- **Decided by:** product owner

## Context
The mockup's CTA is labelled "Secure checkout via Stripe", but no real money may
move: this is a demonstration. Separately, the E2E requirement forbids mocked and
intercepted calls, so a real Stripe redirect would make the suite non-hermetic.

## Decision
- **No payment provider, no Stripe, no card data, no webhook, no sandbox/real
  provider seam, no retries, no dunning, no suspension.** Every payment in this
  app succeeds by construction.
- On "Rent This Setup" a dialog appears stating that no money is taken. The user
  must type `this is a demo` to enable the confirm control.
- Phrase matching is **trimmed and case-insensitive**, otherwise exact.
- Enforced in the browser (control disabled until it matches) **and** on the
  server (a mismatch is rejected), so the order cannot be created by posting
  around the dialog.
- Cancel closes the dialog and leaves the cart untouched.
- On confirmation the order is persisted, an `Invoice` is raised and **settled
  immediately**, the draft is cleared and its token rotated, and the user lands
  on an order confirmation view.
- The dialog is a native `<dialog>` via `showModal()`, styled with `DESIGN.md`
  Level-2 tokens (backdrop-blur 12px, 80% white, 16px radius).
- The Stripe sentence in the mockup footer is replaced with demo-accurate copy.

## Consequences
Invoices remain a real domain concept (amounts snapshotted at issue, 0%-seeded
tax line pipeline), so the totals shown are produced by the same code that would
later settle a real charge. No payment-provider code exists to remove later.
