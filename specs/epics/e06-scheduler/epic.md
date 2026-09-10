# E6 — The month-to-month scheduler

**Slice:** 2 · **Status:** pending · **ADR:** 0011

## Goal
Make "month-to-month" real: something time-driven must advance a rental, because
there is no admin and no operator to do it.

## In scope
- A hosted service driven by an injected **`TimeProvider`** — tests advance a fake
  clock; nothing sleeps.
- Transitions: on payment, schedule delivery at a fixed lead time; on the delivery
  date, activate; on each monthly anniversary, raise the next invoice **and settle
  it immediately**; on a cancellation request, suppress the next invoice and end
  the rental at the end of the already-paid period.
- **Exactly-once** renewal per period, including when the service runs twice
  within one period (idempotency keyed on the period, not on a timestamp).
- Period arithmetic: `anchor.AddMonths(N)` from a stored anchor, so 31 Jan → 28
  Feb → 31 Mar → 30 Apr with no drift.
- Business-date derivation at a fixed **`+08:00`** offset (no DST in Indonesia,
  so no timezone database).
- No retries, no dunning, no suspension: payment cannot fail in this app.

## Out of scope
Refunds, proration, commitment terms, plan changes, pausing.

## Stories
1. As a customer I want my equipment delivered a predictable time after I pay.
2. As the business I want the rental to activate when it is delivered, not when
   it is ordered.
3. As the business I want a renewal invoice every month, exactly once.
4. As a customer I want cancelling to stop the next charge and nothing more.
5. As a developer I want deterministic tests instead of sleeping.

## Definition of done
Build clean; scheduler unit tests green using `FakeTimeProvider`; matrix rows
passing; archived.

## Matrix rows
SC-01…SC-11 · ARC-06 · CO-08

## Risks
- Duplicate renewal invoicing is the single most damaging bug in this epic. The
  idempotency test (SC-04) is not optional.
- Cancellation timing at a period boundary (requested on the last day) must be
  defined and tested rather than left to `if` ordering.
