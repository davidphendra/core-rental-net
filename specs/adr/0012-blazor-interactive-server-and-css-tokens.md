# ADR-0012: Blazor Web App with InteractiveServer; design tokens as CSS variables

- **Status:** Accepted (2026-09-10)
- **Decided by:** architecture

## Context
A server-persisted draft behind an `HttpOnly` cookie plus a file database means
the host must be server-rendered. A WebAssembly-standalone client cannot own the
cookie or reach the database.

## Decision
- Blazor Web App, **`InteractiveServer` globally**. One navigation model, no
  mixed-mode or `enhancedload`/JS-interop re-initialisation edge cases.
- **No public HTTP API.** All interaction is component events over the circuit;
  the only routes are Blazor pages. No JSON contracts, no CORS, no bearer auth.
  The host is therefore `src/Host/…`, not `src/API/…`.
- All `DESIGN.md` tokens become **CSS custom properties** in one `tokens.css`.
  No Tailwind CDN, no copied inline styles, no arbitrary inline `style` for
  colour or spacing.
- Slot geometry lives in one CSS layer keyed by slot id. No pixels in C#.
- Accessibility: **WCAG 2.1 AA intent** via markup discipline — semantic
  landmarks, real `<button>`/`<a>` instead of clickable `<div>`s, accessible
  names, visible focus, `aria-disabled` on disabled controls, alt text. No axe
  gate and no formal audit.
- UI imagery: missing product image files render a design-system placeholder
  tile; the six remotely hosted product images are vendored into `wwwroot` so
  nothing depends on a third-party CDN.

## Consequences
Playwright can reliably interact because controls are real elements. There is no
reusable API for a future native client — an accepted trade.
