# Design System Strategy: The Cinematic Authority

## 1. Overview & Creative North Star
The Creative North Star for this design system is **"The Digital Curator."**

Unlike standard data management tools that feel industrial and cold, this system treats information like a high-end film archive. It moves away from the "template" aesthetic by utilizing intentional asymmetry, deep tonal layering, and high-contrast editorial typography. The goal is to provide a sense of prestige and cinematic depth, transforming a dashboard into a premium workspace that feels as sophisticated as the industry it serves.

By breaking the rigid 12-column grid with overlapping elements and variable-width containers, we create a layout that feels dynamic and "scored" rather than just programmed.

## 2. Colors & Surface Philosophy

This palette is designed to evoke the high-contrast lighting of a film set, utilizing **Indigo Dark** as our "shadow" and **Gold/Amber** as our "key light."

### The Palette (Material Design Tokens)
*   **Primary (Gold):** `#D4AF37` (Container: `#735c00`) – Used for primary actions and moments of prestige.
*   **Secondary (Indigo):** `#1A1B2E` (Container: `#5c5c73`) – Our foundational depth color.
*   **Tertiary (Cyan/Emerald):** `#00FFFF` (Emerald: `#10B981`) – Used strictly for data visualization and technical indicators.
*   **Neutral Surfaces:** From `surface_container_lowest` (#ffffff) to `surface_dim` (#d9d8f2).

### Technical Rules for Color Application
*   **The "No-Line" Rule:** 1px solid borders for sectioning are strictly prohibited. You must define boundaries through background color shifts. Use `surface_container_low` for the main canvas and `surface_container_lowest` for individual cards.
*   **Surface Hierarchy & Nesting:** Treat the UI as a series of physical layers. A "nested" look is achieved by placing a `surface_container_highest` element (like a search bar) inside a `surface_container` area. This creates natural depth without visual clutter.
*   **The "Glass & Gradient" Rule:** Floating panels or high-level navigation (like the sidebar) should utilize a subtle gradient from `primary` to `primary_container` or `secondary` to `on_secondary_container` at 85% opacity with a `20px` backdrop blur (Glassmorphism). This prevents the UI from feeling flat and "pasted on."

## 3. Typography: Editorial Sophistication

We utilize **Inter** as our typographic workhorse. The hierarchy is designed to mimic a film's title sequence—authoritative, clear, and perfectly spaced.

*   **Display (3.5rem):** Reserved for high-level dashboard summaries. Use negative letter-spacing (-0.02em) to create a "locked-in" premium feel.
*   **Headline (2rem - 1.5rem):** Used for page titles and workspace headers. These should always be `on_surface` to maintain high-end readability.
*   **Body (1rem - 0.875rem):** Set with a generous line height (1.6) to ensure the data-heavy management screens remain breathable.
*   **Labels (0.75rem):** All labels must be in **All-Caps** with a `0.05em` letter-spacing to act as "metadata" markers, distinguishing them clearly from content.

## 4. Elevation & Depth

In this design system, depth is a narrative tool. We use **Tonal Layering** instead of structural lines to convey hierarchy.

*   **The Layering Principle:** Place `surface_container_lowest` cards on a `surface_container_low` background. This creates a "soft lift" that feels architectural.
*   **Ambient Shadows:** For floating elements, use a 4-layer diffused shadow.
    *   *Values:* `0px 10px 30px rgba(25, 26, 45, 0.06)`.
    *   The shadow color is never grey; it is a tinted version of `on_surface` to mimic natural light passing through glass.
*   **The "Ghost Border" Fallback:** If a border is required for accessibility in input fields, use the `outline_variant` at 15% opacity. Never use 100% opaque borders.
*   **Glassmorphism:** Use `surface_tint` at low opacities for overlays. This allows the vibrant brand colors (Cyan/Gold) to bleed through the background, maintaining a cohesive "glow."

## 5. Components

### Buttons
*   **Primary (Gold):** Heavyweight. Uses `#D4AF37` with a subtle gradient to `#735c00`. Border-radius: `md` (0.75rem).
*   **Secondary (Indigo):** Outline-only or `secondary_container`. Used for "Manage" or "Edit" actions.
*   **Tertiary:** Text-only with an icon. Used for low-priority actions like "View Details."

### Cards & Data Lists
*   **Prohibition:** Divider lines are forbidden.
*   **The Separation Rule:** Separate list items using 16px of vertical whitespace or a hover state that shifts the background to `surface_container_highest`.
*   **Visual Style:** Cards must have a subtle roundedness (0.75rem). In the "Cinema Management" view, images should be edge-to-edge within the top half of the card to maximize cinematic impact.

### Chips & Status Indicators
*   **Status Chips:** Use `tertiary_container` (Cyan/Emerald) with `on_tertiary_container` text. Keep them pill-shaped (`full` roundedness) to contrast against the cards' subtle roundedness.

### Input Fields
*   **Style:** Minimalist. No bottom line or box—only a subtle `surface_container_highest` background with a `Ghost Border`. Focus state is indicated by a 2px Gold (`primary`) left-side accent.

## 6. Do's and Don'ts

### Do:
*   **Embrace Negative Space:** If a screen feels "busy," increase the padding between containers. High-end UI breathes.
*   **Use Intentional Asymmetry:** In the dashboard, allow a 1/3 vs 2/3 split for the layout to create a curated, editorial feel.
*   **Tint Your Neutrals:** Always ensure your "whites" and "greys" are slightly tinted with the `Indigo Dark` (#1A1B2E) palette to maintain color harmony.

### Don't:
*   **Don't use 1px solid black or grey borders.** It breaks the "Curator" illusion and makes the UI look like a generic spreadsheet.
*   **Don't use standard drop shadows.** Avoid the "fuzzy black" look; if it doesn't look like ambient light, it's too heavy.
*   **Don't crowd the sidebar.** The Indigo Dark sidebar should have high padding-top (48px+) to allow the brand logo to stand alone as a mark of quality.