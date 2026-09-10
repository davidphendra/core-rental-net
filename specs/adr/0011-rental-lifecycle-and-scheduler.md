# ADR-0011: Rolling month-to-month lifecycle with a reduced scheduler

- **Status:** Accepted (2026-09-10)
- **Decided by:** product owner

## Context
The product is month-to-month with *"Cancel anytime"*. With no admin and no
login, no human actor exists to advance a rental's lifecycle, so states would be
unreachable dead code unless something time-driven advances them.

## Decision
- **Rolling month-to-month**, minimum 1 month, renews until cancelled. No
  commitment terms (1/3/6/12) and **no duration selector** is built.
- **No proration.** The first month is billed in full at checkout regardless of
  start date. The anchor is the **order date**.
- Period *N* is computed as `anchor.AddMonths(N)`, stored once. This clamps to
  the month's last day *and* restores the anchor day later (31 Jan → 28 Feb →
  31 Mar), with no special-casing.
- **No deposit or collateral**; modelled as a zero `Money` field.
- **Cancellation** is requestable at any time and takes effect at the **end of
  the currently paid month** — no refunds, the next renewal is suppressed, and
  pick-up is implied.
- A **reduced scheduler** (hosted service, injected `TimeProvider`) owns the
  transitions: auto-schedule delivery at a fixed lead time, activate on that
  date, raise and immediately settle the next invoice on each monthly
  anniversary, and honour the cancellation boundary.
- States: `Draft → Placed → Paid → DeliveryScheduled → Active →
  CancellationRequested → Ended`, plus `Cancelled` before delivery. There is no
  `PaymentFailed` state, because payment cannot fail.
- Business dates are derived in a **fixed `+08:00` (WITA) offset**. Indonesia has
  no daylight saving, so this is a constant rather than a timezone database
  lookup.
- Checkout is **idempotent**: the draft's `Draft → Converted` state is the key,
  and a repeat checkout returns the existing order rather than creating a second.

## Consequences
Renewals, cancellation boundaries and idempotency give the richest unit-test
cluster in the project, all deterministic via `FakeTimeProvider` — no sleeps.
