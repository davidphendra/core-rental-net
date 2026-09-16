# Flaky funnel test — investigation and fix

Test: `HermeticityTests.A_whole_funnel_reaches_nothing_but_the_application`.
Symptom: failed **once**, in one full run of the browser suite; the message was not captured.

## What was observed

| Run | Result |
|---|---|
| Full suite, run 1 | **FAIL** (1 failed, 102 passed) |
| Full suite, runs 2–5 | pass |
| Full suite, runs 6–20 | pass (15 in a loop) |
| The single test alone, runs 1–12 | pass |

That is **1 failure in 33 runs** — roughly 3%, and not reproducible on demand. The isolation runs
passing while the full suite failed points at load or timing rather than at the test's logic.

**The evidence was lost to my own command, not to the repository.** I ran the suite with `-v q`, which
suppresses the failure text; the captured output was five lines. CI does not do this — it runs
`dotnet test tests/CoreRentalNet.E2E --configuration Release --no-build` at normal verbosity and
uploads the browser output as an artifact on failure. So the next occurrence is diagnosable; mine was
not. Run the suite without `-v q` when hunting this.

## The defect found and fixed

`E2ETest` collected the page's requests in a `List<string>`:

```csharp
Page.Request += (_, request) => Requests.Add(request.Url);   // Playwright's thread
...
var foreign = Requests.Where(...)                            // the test's thread
```

The browser reports each request on its own thread while the test enumerates on another, and the page
is still fetching images exactly when the assertion runs — the funnel ends on `/orders/{token}`, which
renders product images. `List<T>` is documented as not safe for concurrent reads and writes
([MS Learn](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1#thread-safety)),
so an add arriving mid-enumeration throws `InvalidOperationException: Collection was modified` — a
failure that names nothing about the application, in a test whose whole purpose is to assert something
about the application.

It is now a `ConcurrentQueue<string>`, read through a snapshot property. Three tests assert on these
requests (`HermeticityTests` twice, `AuthenticationTests` once), so the race had three ways to fire.

## Honest limit

**I cannot prove this was the failure that was observed.** The message is gone, and the fix is a
removal of a race rather than a reproduction. What can be said:

- A real, non-deterministic defect existed in the harness and is fixed.
- It is the only mechanism in the test's own code that can fail intermittently, and it fails at exactly
  the point the funnel test asserts.
- If the flake recurs, it is something else — most plausibly the checkout exceeding Playwright's
  default five-second expectation under load. The next occurrence will say which, because the assertion
  messages print the actual URL or the offending hosts, provided the run is not quiet.

## Baselines

| Baseline | Before | After |
|---|---|---|
| BUILD | 0 warnings, 0 errors | 0 warnings, 0 errors |
| NONBROWSER | 536 passed, 0 failed | 536 passed, 0 failed |
| BROWSER | 103 passed, 1 skipped | 103 passed, 3 runs, 1 skipped |

## What was not changed, deliberately

The five-second expectation in `ToHaveURLAsync` was left alone. Widening a timeout is the classic way
to make a flake disappear without fixing it, and there is no evidence yet that the checkout is slow
rather than wrong.
