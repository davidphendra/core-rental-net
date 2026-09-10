# ADR-0008: Fixed-slot canvas, quantities as metadata, zones derived from data

- **Status:** Accepted (2026-09-10)
- **Decided by:** product owner

## Context
Mockups show a fixed scene: a desk bar, a chair, and dashed placeholder slots at
absolute coordinates (`Add Monitor`, `Add Lamp`, `Place a Plant`) plus titled
zones (`Coffee Station`, `Relax Zone`). The mockups also draw `Outdoor Gear` and
`Garage` tiles that no SKU could fill.

## Decision
Named slots with an accepted subcategory and a maximum quantity:

| Slot | Accepts | Max |
|---|---|---|
| Desk | `desk` | 1 |
| Chair | `chair` | 1 |
| Monitor | `monitor` | **3** |
| Lamp | `lamp` | 1 |
| Plant | `plant` | 1 |
| Coffee Station | `coffee` | 1 |
| Relax Zone | `beanbag` | 1 |

- Selection is single-per-slot and **replaces**; capacity lives in data, not
  conditionals. Maximum assignable units = 9.
- Multi-quantity renders at **one position**: a single item image with a `×N`
  badge and a stepper, so scene geometry never reflows.
- Slot **positions live in CSS** (`slotId → --slot-x/--slot-y/--slot-w/--slot-h`).
  The domain holds semantics only, never pixels.
- Zones are **derived from subcategories present in the catalog**, so a zone that
  no SKU can fill cannot be rendered.
- Two fill paths, both drawn in the mockup: clicking an empty slot opens the
  picker filtered to that slot; clicking a product card assigns to its matching
  slot. Exceeding capacity is refused with a message, never silently replaced.
- Removal: a dedicated `×` per filled slot, plus `−`/`+` steppers. No bulk
  "clear workspace" control.

## Consequences
`Outdoor Gear` and `Garage` are removed (the latter after the partner item was
deleted from the catalog), leaving Coffee Station and Relax Zone. The floating
`Air Conditioned` / `High-Speed` badges are dropped as property amenities, not
rentable objects. Invalid states are unrepresentable rather than merely undrawn.
