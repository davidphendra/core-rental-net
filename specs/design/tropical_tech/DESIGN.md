---
name: Tropical Tech
colors:
  surface: '#f8f9fa'
  surface-dim: '#d9dadb'
  surface-bright: '#f8f9fa'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f3f4f5'
  surface-container: '#edeeef'
  surface-container-high: '#e7e8e9'
  surface-container-highest: '#e1e3e4'
  on-surface: '#191c1d'
  on-surface-variant: '#3d4949'
  inverse-surface: '#2e3132'
  inverse-on-surface: '#f0f1f2'
  outline: '#6d7979'
  outline-variant: '#bcc9c8'
  surface-tint: '#006a6a'
  primary: '#006767'
  on-primary: '#ffffff'
  primary-container: '#008282'
  on-primary-container: '#f3fffe'
  inverse-primary: '#6fd7d6'
  secondary: '#376757'
  on-secondary: '#ffffff'
  secondary-container: '#baeed9'
  on-secondary-container: '#3d6d5d'
  tertiary: '#974400'
  on-tertiary: '#ffffff'
  tertiary-container: '#bb580d'
  on-tertiary-container: '#fffbff'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#8cf3f3'
  primary-fixed-dim: '#6fd7d6'
  on-primary-fixed: '#002020'
  on-primary-fixed-variant: '#004f4f'
  secondary-fixed: '#baeed9'
  secondary-fixed-dim: '#9ed1bd'
  on-secondary-fixed: '#002117'
  on-secondary-fixed-variant: '#1d4f40'
  tertiary-fixed: '#ffdbc9'
  tertiary-fixed-dim: '#ffb68d'
  on-tertiary-fixed: '#331200'
  on-tertiary-fixed-variant: '#763300'
  background: '#f8f9fa'
  on-background: '#191c1d'
  surface-variant: '#e1e3e4'
typography:
  display-lg:
    fontFamily: Plus Jakarta Sans
    fontSize: 48px
    fontWeight: '700'
    lineHeight: 56px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Plus Jakarta Sans
    fontSize: 32px
    fontWeight: '700'
    lineHeight: 40px
    letterSpacing: -0.01em
  headline-lg-mobile:
    fontFamily: Plus Jakarta Sans
    fontSize: 28px
    fontWeight: '700'
    lineHeight: 36px
  headline-md:
    fontFamily: Plus Jakarta Sans
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
  body-lg:
    fontFamily: Plus Jakarta Sans
    fontSize: 18px
    fontWeight: '400'
    lineHeight: 28px
  body-md:
    fontFamily: Plus Jakarta Sans
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  label-md:
    fontFamily: Manrope
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 20px
    letterSpacing: 0.02em
  label-sm:
    fontFamily: Manrope
    fontSize: 12px
    fontWeight: '700'
    lineHeight: 16px
    letterSpacing: 0.05em
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  unit: 8px
  container-max: 1280px
  gutter: 24px
  margin-mobile: 16px
  margin-desktop: 40px
  stack-sm: 8px
  stack-md: 16px
  stack-lg: 32px
  section-gap: 80px
---

## Brand & Style

The design system embodies the "Tropical Tech" aesthetic—a sophisticated blend of high-performance SaaS utility and the vibrant, organic energy of Bali. It is designed for digital nomads and remote teams who value both productivity and the adventurous spirit of island life.

The visual language draws from **Corporate Modernism** for its functional reliability, then infuses it with **Soft Minimalism** and organic influences. The interface should feel breezy and open, utilizing generous whitespace to mimic the openness of a tropical pavilion. While the underlying structure is disciplined and professional, the emotional layer is warm, welcoming, and optimistic. The use of high-fidelity iconography and subtle textures will translate the charm of the hand-drawn workspace sketch into a premium digital experience.

## Colors

The palette is anchored in a professional "Jungle & Sea" foundation with high-energy accents.

- **Primary (Teal):** Used for primary actions, active states, and brand-defining moments. It represents the clarity of Balinese waters.
- **Secondary (Jungle Green):** Used for deep backgrounds, heavy typography, and grounding elements. This provides the "Tech" reliability.
- **Tertiary (Sunset Orange):** A vibrant accent for highlights, notifications, or call-to-action buttons that need to stand out against the greens and teals.
- **Neutrals:** A range of soft grays and off-whites (`#F8F9FA`, `#E9ECEF`) replace pure whites to reduce glare and provide a more "natural" feel.

Color application should follow a 60-30-10 rule: 60% neutral/white, 30% primary/secondary greens and teals, and 10% orange accents.

## Typography

This design system uses **Plus Jakarta Sans** as the primary typeface for its friendly, modern, and slightly rounded geometric character. It strikes the perfect balance between professional and approachable. 

**Manrope** is used for labels, navigation items, and data-heavy micro-copy to provide a structured, technical contrast to the warmth of the headlines.

Scale hierarchy is critical:
- Use **Display** sizes for hero sections and welcoming messages.
- Use **Semi-bold** weights for headlines to ensure they feel "anchored" on the page.
- Maintain generous line heights (1.5x for body) to ensure readability and a sense of "airiness" in the layout.

## Layout & Spacing

The layout utilizes a **12-column fluid grid** for desktop and a **4-column grid** for mobile. The philosophy is "Room to Breathe"—avoiding cramped clusters and ensuring every element has clear white space surrounding it.

- **Desktop:** 12 columns, 24px gutters, and 40px side margins.
- **Mobile:** 4 columns, 16px gutters, and 16px side margins.

Spacing follows an 8px linear scale. Vertical "Stacking" (space between elements in a card or group) should be consistent, while "Section Gaps" should be large (80px+) to clearly delineate different parts of the user journey. The "Workspace Builder" interface (inspired by the reference image) should use a centered, focused layout with auxiliary tools floating in reachable panels.

## Elevation & Depth

To maintain the "Tropical Tech" feel, we avoid heavy, dark shadows. Instead, we use **Tonal Layers** and **Ambient Tinted Shadows**.

- **Level 0 (Base):** Soft neutral (`#F8F9FA`).
- **Level 1 (Cards):** Pure white background with a very soft, diffused shadow tinted with the primary teal (e.g., `rgba(0, 139, 139, 0.08)`).
- **Level 2 (Floating/Popups):** Use a glassmorphism effect (backdrop-blur: 12px) with a semi-transparent white fill (80% opacity) to mimic the look of clean glass in sunlight.
- **Interaction:** On hover, cards should slightly lift (increase shadow blur and decrease Y-offset) to provide a tactile, responsive feel.

## Shapes

The shape language is consistently **Rounded**, mirroring the organic lines of the Balinese landscape. 

- **Standard Elements:** Buttons and input fields use a `0.5rem` (8px) radius.
- **Containers:** Workspace cards and larger layout blocks use `rounded-lg` (16px) or `rounded-xl` (24px) to feel soft and inviting.
- **Interactive Icons:** Small chips or status indicators can be pill-shaped to differentiate them from functional buttons.

## Components

### Buttons
- **Primary:** Solid Teal background with White text. Bold, 16px padding.
- **Secondary:** Ghost style with Teal border and text.
- **CTA:** Sunset Orange for "Book Now" or "Rent" actions to create urgency.

### Cards (Workspace Items)
Inspired by the reference sketch, cards should feature a subtle dashed border (2px, gray) when they are "Empty Slots" and a solid white surface with soft shadows once an item (Chair, Desk) is selected. Use high-quality line-art icons that match the "hand-drawn" feel of the reference but with refined, digital strokes.

### Selection Chips
Rounded pill-shaped tags used for categories (e.g., "Air Conditioned", "Pool Access"). Use light Teal backgrounds with dark Teal text.

### Input Fields
Soft gray backgrounds with a 1px border that turns Teal on focus. Labels should be in Manrope Bold for high legibility.

### Tab System
Clean, horizontal tabs with an underline indicator in Teal. Inspired by the "Chairs / Desks / Accessories" section in the reference, these should feel like a physical folder system but with modern, flat styling.