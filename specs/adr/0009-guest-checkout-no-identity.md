# ADR-0009: Guest checkout; no identity provider for the MVP

- **Status:** Accepted (2026-09-10), **amended by ADR-0016**: the identity provider is no longer
  deferred, though guest checkout and the absence of an administrator both stand
- **Decided by:** product owner

## Context
The design header shows a profile avatar, but no screen designs registration,
login, or an account area. An identity provider is a large build.

## Decision
- **No Identity/Access module, no login, no admin, no back office.**
- Checkout is guest-only.
- Orders are **anonymous**: the mockups contain a single `Delivery Location`
  input and no name, email or phone field, and there is no consumer for a
  contact in this MVP — no notification email is sent and there is no admin to
  read it. Adding fields nothing reads was rejected as unnecessary work.
- Post-checkout, an order is reached through an **opaque access token stored
  hashed**, not a number.
- Order references are consequently not security material: `CR-YYYY-NNNN` and
  `INV-YYYY-NNNN` are display labels. A guessed number without a token returns
  not found.

## Consequences
- No "My Rentals", cancel or extend UI. The domain still retains the state needed
  to add them.
- When identity arrives, contact capture returns as a migration plus changes to
  the checkout command and confirmation view — a small, understood change.
