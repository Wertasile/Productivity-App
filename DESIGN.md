# Design System Specification: High-End Digital Editorial
 
## 1. Overview & Creative North Star
It aims to bridge the gap between the tactile, romanticized history of physical stationery and the fluid efficiency of modern digital interfaces. 
 
Unlike standard "flat" SaaS applications, this system rejects the industrial rigidity of 1px borders and perfect geometric circles. Instead, it embraces **Intentional Asymmetry** and **Tonal Depth**. The goal is to make the user feel as though they are interacting with a curated desk—layered with vellum, weighted by premium paper stock, and organized through subtle shifts in light rather than structural dividers.
 
## 2. Colors
Our palette is rooted in a warm, low-strain vellum base. It avoids pure whites (#FFFFFF) and harsh blacks (#000000) to maintain a premium, editorial feel.
 
### Color Tokens
*   **Background (Vellum):** `surface` (#fefcf1) - The primary canvas.
*   **Primary CTA Color:** `primary` (#69663c) - A deep, olive-toned ink used for key actions.
*   **Primary text color:** `on_primary_container` (#37392c) - A deep, olive-toned ink used for key actions.
*   **Task Primary color:** `task_color` (#504BEF) - A deep bluish-vilot colour, use to indicate TASK CTA icons/buttons.
*   **Reminder Primary color** `reminder_color` (#F67ADF) - A pink colour, used to indicate Reminder CTA icons/buttons.
*   **Tertiary Accents (The Note Palette):**
    *   `primary_container` (#FFF9C4) - Solar Yellow (Soft Sticky)
    *   `tertiary_container` (#FCE4EC) - Petal Pink (Soft Sticky)
    *   `secondary_container` (#E3F2FD) - Sky Blue (Soft Sticky)
 
### The "No-Line" Rule
**Explicit Instruction:** Do not use 1px solid borders to section content. Boundaries must be defined through background shifts. To separate a sidebar from a main stage, transition from `surface` to `surface_container_low`. Use white space (Token `12` or `16`) to provide breathing room rather than a line.
 
### Surface Hierarchy & Nesting
Treat the UI as physical layers of paper. 
1.  **Level 0 (The Desk):** `surface_dim` (#e4e4d1)
2.  **Level 1 (The Notebook):** `surface` (#fefcf1)
3.  **Level 2 (The Sheet):** `surface_container_low` (#fbfaec)
4.  **Level 3 (The Card):** `surface_container_lowest` (#ffffff) - Used for cards that need to "pop" off the vellum.
 
### Signature Textures
Apply a subtle noise grain (2-3% opacity) over the `surface` tokens to simulate paper pulp. For main CTAs, use a gentle gradient from `primary` to `primary_dim` to provide "ink-like" weight.
 
## 3. Typography
The system uses a high-contrast pairing: a traditional serif for "The Voice" and a clean neo-grotesque for "The Content."
 
*   **The Voice (Crimson Text/Newsreader):** Used for all `display` and `headline` tokens. This conveys authority and historical weight.
*   **The Content (Manrope):** Used for `title`, `body`, and `label` tokens. It ensures high legibility and a modern, functional feel for note-taking and metadata.
 
### Typography Scale
*   **Display Large:** `display-lg` (48px) - Used for notebook titles.
    *   Font: Newsreader, Italic, 48px
    *   Line-height: default
    *   Letter-spacing: 0px
*   **Display Heading 1 :** `h1` (24px) - Used as headings for pages / sections.
    *  Font: Newsreader, Bold, 24px 
    *  Line-height: 32px
    *  Letter-spacing: 0px
*   **Display Heading 2:** `h2` (20px) - Used for subheadings within pages.
    *  Font: Newsreader, Medium, 20px
    *  Line-height: 28px
    *  Letter-spacing: 0px
*  **Display Heading 3:** `h3` (18px) - Used for subheadings within pages.
    *  Font: Newsreader, Medium, 18px 
    *  Line-height: 26px
    *  Letter-spacing: 0px

*  **Standard Text:** `body` (13px) - Used for the main content of notes and descriptions.
    *  Font: Manrope, Regular, 13px
    *  Line-height: default
    *  Letter-spacing: 0px
  
*  **Sub Headings:** `title` (1.125rem) - Used for section headers within notes.  
    *  Font: Manrope, Semi-Bold, 1.125rem
    *  Line-height: default
    *  Letter-spacing: 0px
  
    *  **Label Medium:** `label-md` (0.8125rem) - Used for metadata like dates or tags.
    *  Font: Manrope, Medium, 0.8125rem
    *  Line-height: default
    *  Letter-spacing: 0px
*   **Label Small:** `label-sm` (0.6875rem) - Used for "taped" labels or dates.
 
## 4. Elevation & Depth
Depth in this system is achieved through **Tonal Layering** and **Ambient Light Simulation**.
 
*   **The Layering Principle:** Instead of shadows, nest containers. A `surface_container_highest` element placed inside a `surface` background creates a natural focal point without visual noise.
*   **Organic Shadows:** When elements must float (like a sticky note), use an extra-diffused shadow: `box-shadow: 0 10px 30px rgba(55, 57, 44, 0.06);`. The shadow color is a tint of `on_surface`, not grey.
*   **The "Ghost Border":** If a boundary is strictly required for accessibility, use `outline_variant` (#babaa9) at **15% opacity**. It should be felt, not seen.
*   **Tactile Accents:** Use the `xl` (1.5rem) roundness token for cards, but apply a slight `transform: rotate(-0.5deg)` to select items to mimic a hand-placed paper effect.
 
## 5. Components
 
### Buttons & Chips
*   **Primary Button:** Uses `primary` background with `on_primary` text. Roundness: `full` (9999px). No shadow; use a 2px offset on hover to simulate "pressing" into the paper.
*   **The "Taped" Chip:** To represent tags or pinned items, use `secondary_container` with a `sm` (0.25rem) corner radius to look like a strip of washi tape.
 
### Input Fields
*   **Text Inputs:** No bottom line or box. Use a subtle `surface_container_low` background and `title-md` typography. The focus state should simply deepen the background tint to `surface_container_high`.
 
### Cards (Notes & Folders)
*   **The Folder:** Use `primary_container` (#eae4b1). Forbid divider lines. Separate the folder icon from the text using Spacing Token `3`.
*   **The Sticky Note:** Use the vibrant tertiary palette. Apply a `xl` corner radius on three corners and a `sm` radius on the top-right to simulate a "folded" or "dog-eared" corner.
*   **Taped/Pinned Effect:** Use a small overlay element (10px x 40px) with 30% opacity `surface_container_highest` at the top of a card to mimic clear scotch tape.
 
### Navigation
*   **Sidebar:** Use `surface_container` (#f5f4e5). Icons should be oversized (20px) with `on_surface_variant` colors. Active states are indicated by the the active state text being in bold text, never by an underline.

### Modals
*   **Modal:** Model should be the same as the card ui with white back ground and taped/pinned affect like the cards(notes). Model should be centered on the screen and should have a close button in the top-right corner. Size of the model is around 500px width and 500px height. Title font must be Newsreader, regular font weight, 28px with 37.5px line height.

## 6. Do's and Don'ts
 
### Do
*   **Do** use asymmetrical spacing. If the left margin is `12`, try a right margin of `16` for an editorial, non-templated look.
*   **Do** use `Newsreader/Crimson Text` for numbers to give dates and counts a premium feel.
*   **Do** prioritize vertical white space over lines. If content feels cluttered, increase the spacing token rather than adding a divider.
 
### Don't
*   **Don't** use pure black (#000000). Use `on_background` (#37392c) for all "black" text.
*   **Don't** use standard `0.5rem` roundness for everything. Mix `xl` for containers and `full` for interactive pills to create visual rhythm.
*   **Don't** use high-contrast shadows. If a shadow is clearly visible as a dark smudge, it is too heavy. It should appear as a soft "glow" of depth.