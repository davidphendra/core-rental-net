# E7 verification — the committed browser suite

`tests/CoreRentalNet.E2E`, 43 tests, all passing, in about nine seconds.

## How it runs

- A **real Kestrel process**, started the way a person starts it, on a free port, from the
  application's own directory.
- Its own **throwaway database** in a temporary directory, migrated by the application itself and
  deleted afterwards.
- **One browser context per test**, which is one browser profile, which is one draft cookie. That
  is how tests stay independent without restarting the application between them.
- Chromium, plus a mobile-viewport class at 390×844.
- **No interception.** No route handlers, no stubbed responses, no service worker. One test asserts
  the absence of a worker and that a direct fetch from the page reaches the server.
- Readiness is not "the port answers". It requires the root page **and** a real static asset, so a
  build started from the wrong directory cannot pass as healthy.

## What it covers

The whole funnel and the parts of it only a browser can reach: the canvas and its capacity rules,
the filtered slot picker, the stepper bounds, removal, the demo dialog and its phrase gate, the
cart reset, the confirmation page and a guessed link, the mobile bottom navigation and its gating,
keyboard-only assignment, the dialog's native focus behaviour, accessible names on every control,
and that a whole funnel reaches nothing but the application — no third party, no external font, no
remote product image.

## The application bugs it found

1. **An accessibility attribute rendered as text.** `aria-disabled="@(!CanCheckout).ToString().ToLowerInvariant()"`
   — Razor closed the expression at the closing parenthesis, so the attribute became
   `True.ToString().ToLowerInvariant()`. Four other attributes used bare property chains that would
   break identically the moment a method call was appended. All are now fully wrapped, and the
   suite asserts the resulting attribute value.
2. **The readiness signal matched prerendered markup.** `.workspace-stage` exists in the prerendered
   HTML, so clicking during the window before the circuit attaches did nothing and the test failed
   for the wrong reason. The layouts now carry a `data-interactive` marker that only the interactive
   render sets to `true`.
3. **A redirect produced a server error.** `/builder?empty=1` cannot bind to a `bool`; the binder
   needs `empty=true`. The E3 verification recorded the 302 but never followed it, so this survived
   for two epics.

## A bug in the suite itself, worth recording

The fixture first started the application from its **build output** directory. A development build
serves its static assets from the project directory, so every stylesheet, font and vendored image
was a 404 while every test still passed — the pages had the right markup and no styling. The
product-images test caught it. Readiness now checks a real asset, so this cannot recur quietly.

## Test counts

| Layer | Tests |
|---|---|
| Unit (BuildingBlocks, Catalog, Workspace, Rentals) | 207 |
| Integration, against real SQLite and the real catalog file | 54 |
| Architecture | 21 |
| Browser | 43 |
| **Total** | **325** |
