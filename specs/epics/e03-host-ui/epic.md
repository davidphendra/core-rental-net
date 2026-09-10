# E3 — Host and the four designed screens

**Slice:** 1 · **Status:** ready · **ADR:** 0007, 0012

## Goal
The four provided screens, rendered from the design tokens, driving the draft
through the funnel — with the canvas geometry living in CSS and no pixels in C#.

## In scope
- Blazor Web App host, `InteractiveServer` globally; composition root registering
  all modules. **No public HTTP API**; the only non-Blazor surface is the draft
  token middleware.
- `tokens.css`: every colour, radius, spacing and type scale from
  `specs/design/tropical_tech/DESIGN.md` as CSS custom properties. No Tailwind
  CDN, no copied inline styles.
- `slots.css`: `slotId → --slot-x / --slot-y / --slot-w / --slot-h`; one layer
  retunes the scene.
- `MarketingLayout`, `AppHeader`, `MobileBottomNav`, `BuilderShell`.
- **Home** (`/`) — hero, the two `badge: popular` cards, "How it Works".
- **Builder** (`/builder`) — side panel with category nav (Chairs, Desks,
  Accessories, Extras), product grid, slot canvas with dashed empty slots, the
  `×N` + stepper treatment for quantity, running `Monthly Total`, floating
  `Ready to Rent?`, `View Setup Summary`.
- **Store** (`/extras`) — extras and accessories listings with "Add to Setup".
- **Review** (`/review`) — quote, itemised lines with quantities, one-time
  delivery fee, grand total, two-line address textarea, gated CTA.
- Gating rules; `ProductImage` placeholder state; the 6 remote images vendored
  into `wwwroot/images/vendored/`.
- Accessibility by markup: real `button`/`a`, accessible names, visible focus,
  landmarks, `aria-disabled`, alt text.

## Out of scope
The demo dialog and order creation (E5). Any drag-and-drop or free placement.

## Stories
1. As a customer I want a page that looks like the provided design, with the
   palette, type and radii coming from one token file.
2. As a customer I want to browse by category and click a product to assign it.
3. As a customer I want to click an empty slot to open the picker already
   filtered to that slot.
4. As a customer I want to see the running monthly total update as I compose.
5. As a customer on a phone I want the bottom nav and collapsible panel.
6. As a customer I want an item with no image file to look deliberate rather
   than broken.
7. As a keyboard user I want to complete the funnel without a mouse.

## Definition of done
Build clean; matrix rows passing; capsule archived.

## Matrix rows
NAV-01…NAV-07 · UI-01…UI-08 · SLOT-04 · ADDR-06 · WS-08 · WS-10

## Risks
- The mockups render interactive slots as clickable `div`s. Using real buttons
  changes no visual but is required both for accessibility and for Playwright to
  interact reliably.
- E2E must not depend on a third-party CDN, hence vendoring.
