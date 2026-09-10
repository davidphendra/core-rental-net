# E6 verification — the time-driven run

Two kinds of evidence: deterministic tests that move a clock, and the hosted service observed
running in the real application.

## In the running application

Placed a real order through the browser, restarted the application, and read the database:

```
Scheduler DeliveryScheduled for CR-2026-0001
Scheduler pass for 09/10/2026: 1 scheduled, 0 activated, 0 invoiced, 0 ended

order CR-2026-0001  status 3 (DeliveryScheduled)  delivery 2026-09-12  activated None
invoices: 1
```

The order was `Paid` immediately after checkout. One pass of the hosted service moved it to
`DeliveryScheduled` with a delivery date two days after the order — which is the delivery lead
time, and the only thing that can advance an order with no administrator.

## In tests

14 unit tests move a `FakeTimeProvider` or pass a date directly; nothing sleeps.

| Behaviour | Result |
|---|---|
| A paid order is scheduled at the lead time | PASS |
| It activates on the delivery date and not before | PASS |
| A renewal is raised and settled when the next month begins | PASS |
| Running again for the same period raises nothing | PASS |
| The month paid at checkout is never billed twice | PASS |
| A three-month gap catches up one invoice per period that started | PASS |
| Cancelling suppresses the next renewal | PASS |
| A cancelled order never bills a period past its end | PASS |
| It ends on the paid boundary and not before | PASS |
| A 31 January order renews on 28 February and returns to the 31st | PASS |
| A finished order is left alone | PASS |
| A pass with nothing to do changes nothing | PASS |

Integration tests against a real database add: the renewal row really is written, five repeated
passes still produce two invoices, cancelling leaves one, and the unique index on
(rental, period) refuses a duplicate even if the scheduler's own check were bypassed.

## The bug this found

The scheduler only saved when the **order** had changed:

```csharp
changed |= rental.Version != before;   // issuing an invoice does not touch the order
if (changed) { await unitOfWork.SaveChangesAsync(...); }
```

Issuing an invoice does not change the rental, so a pass whose only work was billing wrote
**nothing at all** — every renewal was silently lost. The unit tests could not see it because the
in-memory invoice repository records an addition whether or not anything is saved; only the
database showed it. The pass now saves unconditionally.

A second lesson, recorded in the test: an assertion that read *"a pass with no work must not
write"* was asserting the bug. It now states the real invariant — the order and its invoices are
unchanged — rather than naming an implementation detail.
