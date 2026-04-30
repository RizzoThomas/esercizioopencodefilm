# Ferrari Design System — Total Graphic Refactor

## TL;DR

> **Quick Summary**: Trasformazione visuale totale di CineBase dal design Lamborghini/Gold al design system Ferrari (rosso corsa, near-black canvas, angoli netti, full-bleed hero, no ombre).
>
> **Deliverables**:
> - CSS custom properties Ferrari tokens (colors, typography, spacing, rounded)
> - Tailwind config completamente riscritto
> - `styles.css` completamente riscritto (~800 righe, da ~1530)
> - `cyber.css` eliminato
> - Tutte le 17 pagine HTML + 3 component template aggiornate al nuovo design
>
> **Estimated Effort**: Large (21 pagine HTML, 2 CSS, 1 Tailwind config)
> **Parallel Execution**: YES — 4 waves + final
> **Critical Path**: CSS Foundation → Componenti core → Pagine content → Pagine customer → QA finale

---

## Context

### Original Request
"Refactor TOTALE della grafica usando il nuovo DESIGN.md (Ferrari design system)"

### Interview Summary
**Key Decisions**:
- Font: Inter è il substitute accettabile per FerrariSans (display 500, body 400, button 700)
- Tema: Solo scuro (canvas #181818) — niente toggler light/dark
- Hero: Full-bleed cinematic Ferrari-style (immagine viewport, titolo fluttuante in basso)
- Test: QA agent con screenshot per ogni task
- cyber.css: Rimosso completamente

### Research Findings
- Attuale design system: oro (#D4AF37), indaco (#1A1B2E), angoli arrotondati, glassmorphism, ombre
- 21 pagine HTML statiche servite da ASP.NET Core 9.0
- 3 component template caricati dinamicamente (navbar-landing, footer-landing, footer-admin)
- 16 page-specific JS files + 12 core JS modules
- Tailwind CSS via CDN con custom config che referenzia var(--brand-*)

---

## Work Objectives

### Core Objective
Applicare il Ferrari Design System (`DESIGN.md`) a tutte le superfici grafiche di CineBase, sostituendo completamente l'attuale tema Lamborghini/Gold.

### Concrete Deliverables
- `frontend/CineBase.Web/wwwroot/css/styles.css` — riscritto con Ferrari tokens
- `frontend/CineBase.Web/wwwroot/js/tailwind-config.js` — riscritto con Ferrari colors
- `frontend/CineBase.Web/wwwroot/css/cyber.css` — eliminato
- 17 file HTML aggiornati con Ferrari classi e componenti
- 3 component template aggiornati

### Must Have
- Rosso Corsa (#da291c) come unico colore primario di marca
- Canvas near-black (#181818) come sfondo principale
- Angoli netti (0px) su tutti i CTA, card, bande
- Uppercase tracking (1.4px) su tutti i CTA
- Uppercase tracking (0.65px) su nav link
- Full-bleed cinematic hero sulla index page
- Display weight 500 (mai bold)
- Nessuna ombra, nessun glassmorphism

### Must NOT Have (Guardrails)
- Nessun oro (#D4AF37) o colore Lamborghini
- Nessun angolo arrotondato su bottoni/card (solo badge pill e input 4px)
- Nessun drop shadow tier
- Nessun glassmorphism/blur effect
- Nessuna modifica a JS files
- Nessuna modifica al backend
- Nessuna animazione

---

## Verification Strategy

> **ZERO HUMAN INTERVENTION** — Verifica tramite screenshot agent. Ogni task genera uno screenshot della pagina renderizzata.

### Test Decision
- **Infrastructure exists**: NO (test infrastructure not needed)
- **Automated tests**: NO (grafica pura)
- **Agent QA**: Screenshot comparison per ogni pagina HTML

### QA Policy
Every task: Generare screenshot via Playwright della/e pagina/e modificate, salvando in `.sisyphus/evidence/task-N-nome-pagina.png`.

---

## Execution Strategy

### Parallel Execution Waves

```
Wave 1 (Foundation — CSS + Config):
├── Task 1: Ferrari CSS custom properties + tokens
├── Task 2: Tailwind config riscritto
└── Task 3: styles.css riscritto + cyber.css eliminato

Wave 2 (Componenti Core — dipende da Wave 1):
├── Task 4: Navbar → Ferrari top-nav-on-dark
├── Task 5: Footer → Ferrari footer-dark
├── Task 6: index.html hero full-bleed
└── Task 7: dashboard.html admin layout

Wave 3 (Pagine Contenuto — dipende da Wave 1, parallelo a Wave 2):
├── Task 8: films.html + scheda-film.html
├── Task 9: programmazione.html
├── Task 10: proiezioni.html
├── Task 11: categorie.html + registi.html
└── Task 12: cinemas.html + my-cinemas.html

Wave 4 (Pagine Customer — dipende da Wave 1):
├── Task 13: login.html + registrazione.html
├── Task 14: acquista.html (seat map)
├── Task 15: pagamento.html + esito-acquisto.html
├── Task 16: profilo.html
└── Task 17: tmdb-search.html

Wave FINAL (Verifica — dopo tutti i task):
├── Task F1: Plan compliance audit (oracle)
├── Task F2: Full QA — screenshot di TUTTE le 21 pagine
└── Task F3: Scope fidelity check
```

### Agent Dispatch Summary

- **Wave 1**: Task 1 → `quick`, Task 2 → `quick`, Task 3 → `unspecified-high`
- **Wave 2**: Task 4 → `visual-engineering`, Task 5 → `visual-engineering`, Task 6 → `visual-engineering`, Task 7 → `visual-engineering`
- **Wave 3**: Task 8-12 → `visual-engineering` ciascuna
- **Wave 4**: Task 13-17 → `visual-engineering` ciascuna
- **FINAL**: Task F1 → `oracle`, Task F2 → `unspecified-high` + Playwright, Task F3 → `deep`

---

## TODOs

- [x] 1. **CSS Custom Properties — Ferrari Design Tokens**

  **What to do**:
  - Sostituire completamente le variabili CSS in `frontend/CineBase.Web/wwwroot/css/styles.css` da Lamborghini a Ferrari tokens
  - RIMUOVERE: `--brand-gold`, `--brand-gold-dark`, `--brand-gold-light`, `--brand-indigo`, `--brand-indigo-light`, `--brand-cyan`, `--brand-cyan-light`, `--brand-emerald`, `--brand-sidebar-*`, `--brand-shadow`, `--brand-shadow-sm`, `--brand-overlay-*`
  - AGGIUNGERE Ferrari colors dal DESIGN.md:
    - `--ferrari-primary: #da291c` (Rosso Corsa)
    - `--ferrari-primary-active: #b01e0a`
    - `--ferrari-primary-hover: #9d2211`
    - `--ferrari-ink: #ffffff`
    - `--ferrari-body: #969696`
    - `--ferrari-body-strong: #ffffff`
    - `--ferrari-body-on-light: #181818` (solo per future bande editoriali)
    - `--ferrari-muted: #666666`
    - `--ferrari-muted-soft: #8f8f8f`
    - `--ferrari-hairline: #303030`
    - `--ferrari-hairline-on-light: #d2d2d2`
    - `--ferrari-canvas: #181818`
    - `--ferrari-canvas-elevated: #303030`
    - `--ferrari-on-primary: #ffffff`
  - AGGIUNGERE tipografia Ferrari:
    - `--ferrari-font: 'Inter', -apple-system, system-ui, sans-serif`
  - AGGIUNGERE spacing tokens:
    - `--space-xxxs: 4px; --space-xxs: 8px; --space-xs: 16px; --space-sm: 24px; --space-md: 32px; --space-lg: 48px; --space-xl: 64px; --space-xxl: 96px; --space-super: 128px;`
  - AGGIUNGERE rounded tokens:
    - `--rounded-none: 0px; --rounded-xs: 2px; --rounded-sm: 4px; --rounded-md: 6px; --rounded-lg: 8px; --rounded-xl: 12px; --rounded-full: 9999px;`
  - RIMUOVERE tutto il blocco `.dark { }` (solo tema scuro)
  - RIMUOVERE le varianti light mode
  - **Salvare solo le Ferrari tokens, nessuna classe CSS qui** (le classi vanno in styles.css)

  **Must NOT do**:
  - Non lasciare tracce di `--brand-gold`, `--brand-indigo`
  - Non includere light theme variables

  **Recommended Agent Profile**:
  - Category: `quick`
  - Skills: `[]`
  - Reason: Trivial token replacement task

  **Parallelization**:
  - Can Run In Parallel: NO (foundation per tutti gli altri task)
  - Blocks: Tasks 2-17
  - Blocked By: None

  **References**:
  - `frontend/CineBase.Web/wwwroot/css/styles.css:1-117` — Current token block to replace
  - `DESIGN.md:6-136` — Ferrari color, typography, rounded, spacing tokens

  **Acceptance Criteria**:
  - [ ] styles.css ha solo Ferrari tokens, nessun brand-gold/indigo
  - [ ] `--ferrari-primary: #da291c` presente
  - [ ] `--ferrari-canvas: #181818` presente
  - [ ] Nessun blocco `.dark` o light mode

  **QA Scenarios**:
  ```
  Scenario: Ferrari tokens presence
    Tool: Bash
    Preconditions: styles.css file exists
    Steps:
      1. grep '--ferrari-primary: #da291c' styles.css → match found
      2. grep '--ferrari-canvas: #181818' → match found
      3. grep '--brand-gold' → NO match (assert empty)
      4. grep '--brand-indigo' → NO match (assert empty)
      5. grep '--space-xxs: 8px' → match found
    Expected Result: All Ferrari tokens present, no Lamborghini tokens
    Evidence: .sisyphus/evidence/task-1-tokens.txt
  ```

  **Commit**: YES (with Task 2, 3)
  - Message: `feat(css): Ferrari design system tokens`
  - Files: `frontend/CineBase.Web/wwwroot/css/styles.css`

---

- [x] 2. **Tailwind Config — Ferrari Colors**

  **What to do**:
  - Riscrivere `frontend/CineBase.Web/wwwroot/js/tailwind-config.js` per usare i nuovi Ferrari tokens
  - Sostituire `brand.*` con `ferrari.*`
  - Mappare:
    - `ferrari-primary` → `var(--ferrari-primary)`
    - `ferrari-canvas` → `var(--ferrari-canvas)`
    - `ferrari-canvas-elevated` → `var(--ferrari-canvas-elevated)`
    - `ferrari-ink` → `var(--ferrari-ink)`
    - `ferrari-body` → `var(--ferrari-body)`
    - `ferrari-body-strong` → `var(--ferrari-body-strong)`
    - `ferrari-muted` → `var(--ferrari-muted)`
    - `ferrari-hairline` → `var(--ferrari-hairline)`
    - `ferrari-on-primary` → `var(--ferrari-on-primary)`
    - `ferrari-semantic-info` → `var(--ferrari-semantic-info)`
    - `ferrari-semantic-success` → `var(--ferrari-semantic-success)`
    - `ferrari-semantic-warning` → `var(--ferrari-semantic-warning)`

  **Must NOT do**:
  - Non mantenere vecchie chiavi `brand-*`

  **Recommended Agent Profile**:
  - Category: `quick`
  - Skills: `[]`

  **Parallelization**:
  - Can Run In Parallel: YES with Task 3
  - Blocks: Tasks 4-17
  - Blocked By: Task 1

  **References**:
  - `frontend/CineBase.Web/wwwroot/js/tailwind-config.js` — Current config to rewrite
  - `DESIGN.md:6-31` — Ferrari colors

  **Acceptance Criteria**:
  - [ ] tailwind-config.js ha solo `ferrari-*` chiavi
  - [ ] Nessun `brand-*` rimasto

  **QA Scenarios**:
  ```
  Scenario: Ferrari tailwind config
    Tool: Bash
    Preconditions: tailwind-config.js exists
    Steps:
      1. grep 'ferrari-primary' tailwind-config.js → match found
      2. grep 'brand-gold' tailwind-config.js → NO match
    Expected Result: Config uses Ferrari colors
    Evidence: .sisyphus/evidence/task-2-tailwind-config.txt
  ```

  **Commit**: YES (with Task 1, 3)

---

- [x] 3. **styles.css Riscritto + cyber.css Eliminato**

  **What to do**:
  - Riscrivere COMPLETAMENTE `frontend/CineBase.Web/wwwroot/css/styles.css` (da ~1530 righe a ~800)
  - STRUTTURA NUOVA di styles.css:
    1. CSS Custom Properties (da Task 1, verificare presenza)
    2. Base Styles: body background `var(--ferrari-canvas)`, color `var(--ferrari-ink)`, font-family `var(--ferrari-font)`, padding-top 64px (navbar)
    3. Typography classes:
       - `.ferrari-display-mega` — 80px/500/1.05/-1.6px
       - `.ferrari-display-xl` — 56px/500/1.1/-1.12px
       - `.ferrari-display-lg` — 36px/500/1.2/-0.36px
       - `.ferrari-display-md` — 26px/500/1.5/0.195px
       - `.ferrari-title-md` — 18px/700/1.2/0
       - `.ferrari-title-sm` — 16px/500/1.4/0.08px
       - `.ferrari-body-md` — 14px/400/1.5/0
       - `.ferrari-body-sm` — 13px/400/1.5/0
       - `.ferrari-caption` — 12px/400/1.4/0
       - `.ferrari-caption-uppercase` — 11px/600/1.4/1.1px uppercase
       - `.ferrari-button` — 14px/700/1.0/1.4px uppercase
       - `.ferrari-nav-link` — 13px/600/1.4/0.65px uppercase
    4. Button classes:
       - `.btn-primary` — bg `var(--ferrari-primary)`, color white, `var(--ferrari-button)`, padding 14px 32px, height 48px, border-radius 0
       - `.btn-primary:hover` — bg `var(--ferrari-primary-hover)`
       - `.btn-primary:active` — bg `var(--ferrari-primary-active)`
       - `.btn-outline` — transparent, 1px solid white, color white, `var(--ferrari-button)`, padding 13px 31px, height 48px, border-radius 0
       - `.btn-outline:hover` — bg rgba(255,255,255,0.08)
       - `.btn-tertiary` — transparent, color `var(--ferrari-ink)`, `var(--ferrari-button)`
    5. Navbar:
       - `.navbar-ferrari` — bg `var(--ferrari-canvas)`, height 64px, border-bottom 1px solid `var(--ferrari-hairline)`
       - `.nav-link-ferrari` — `var(--ferrari-nav-link)`, color `var(--ferrari-ink)`
    6. Hero:
       - `.hero-cinema` — position relative, min-height 100vh o 560px
       - `.hero-cinema-overlay` — gradient overlay
       - `.hero-cinema-content` — position absolute bottom, padding spacing tokens
       - `.hero-cinema-title` — `var(--ferrari-display-mega)`
    7. Cards:
       - `.card-ferrari` — bg `var(--ferrari-canvas-elevated)`, border-radius 0
       - `.card-ferrari-light` — bg `var(--ferrari-canvas)`, border 1px `var(--ferrari-hairline)`, border-radius 0
    8. Form inputs:
       - `.input-ferrari` — bg `var(--ferrari-canvas)`, border 1px `var(--ferrari-hairline)`, border-radius 4px, padding 14px 16px, height 48px, color `var(--ferrari-ink)`
       - `.input-ferrari:focus` — border-color `var(--ferrari-primary)`
    9. Badge pill:
       - `.badge-ferrari` — bg `var(--ferrari-canvas-elevated)`, color `var(--ferrari-ink)`, `var(--ferrari-caption-uppercase)`, border-radius 9999px, padding 4px 12px
    10. Livery band:
        - `.livery-band` — bg `var(--ferrari-primary)`, color `var(--ferrari-ink)`, `var(--ferrari-display-lg)`, padding 96px
    11. Footer:
        - `.footer-ferrari` — bg `var(--ferrari-canvas)`, color `var(--ferrari-body)`, padding 64px 48px
        - `.footer-link-ferrari` — color `var(--ferrari-body)`, `var(--ferrari-body-sm)`
    12. Utility classes:
        - `.text-primary` → color `var(--ferrari-primary)`
        - `.text-ink` → color `var(--ferrari-ink)`
        - `.text-body` → color `var(--ferrari-body)`
        - `.text-muted` → color `var(--ferrari-muted)`
        - `.bg-canvas` → bg `var(--ferrari-canvas)`
        - `.bg-canvas-elevated` → bg `var(--ferrari-canvas-elevated)`
        - `.border-hairline` → border 1px solid `var(--ferrari-hairline)`
    13. Scrollbar styling (slim, mantenere)
    14. Animation: `.animate-fade-in` (mantenere, è generico)
  - ELIMINARE completamente `frontend/CineBase.Web/wwwroot/css/cyber.css`
  - RIMUOVERE: glass-panel, card-elevated, card-container, ghost-input classi e variabili associate
  - RIMUOVERE: brand-shadow, brand-shadow-sm
  - RIMUOVERE: sidebar-glass, sidebar-*
  - RIMUOVERE: row-hover, table-row-hover, btn-page (non Ferrari)
  - RIMUOVERE: chip-status, chip-active, chip-past (rifare come badge-ferrari)
  - RIMUOVERE: theme-toggle, theme-transition (solo dark mode)
  - RIMUOVERE: tutto ciò che referenzia brand-gold, brand-indigo, brand-cyan, brand-emerald
  - MANTENERE (ma rinominare per coerenza): seat-map styles, date-rail, show-time, payment-option, film-schedule-card

  **Must NOT do**:
  - Non lasciare glass-panel, ghost-input, card-elevated, btn-gold classi
  - Non mantenere variabili light/dark theme

  **Recommended Agent Profile**:
  - Category: `unspecified-high`
  - Skills: `[]`
  - Reason: Large CSS rewrite, many classes to replace

  **Parallelization**:
  - Can Run In Parallel: YES with Task 2
  - Blocks: Tasks 4-17
  - Blocked By: Task 1

  **References**:
  - `frontend/CineBase.Web/wwwroot/css/styles.css` — Full file to rewrite
  - `frontend/CineBase.Web/wwwroot/css/cyber.css` — File to delete
  - `DESIGN.md:1-268` — Full Ferrari design spec
  - `DESIGN.md:33-115` — Typography tokens
  - `DESIGN.md:137-270` — Component definitions

  **Acceptance Criteria**:
  - [ ] styles.css non contiene brand-gold, brand-indigo, brand-shadow, glass-panel, ghost-input, card-elevated, btn-gold, sidebar-*
  - [ ] styles.css contiene classi .btn-primary, .btn-outline, .card-ferrari, .input-ferrari, .badge-ferrari, .navbar-ferrari, .hero-cinema, .livery-band, .footer-ferrari
  - [ ] cyber.css NON esiste più
  - [ ] styles.css è ~800 righe o meno

  **QA Scenarios**:
  ```
  Scenario: No Lamborghini CSS traces
    Tool: Bash
    Preconditions: styles.css written, cyber.css deleted
    Steps:
      1. grep -c 'brand-gold\|brand-indigo\|glass-panel\|ghost-input\|card-elevated\|btn-gold\|sidebar-\|brand-shadow' styles.css → 0 matches
      2. test -f cyber.css → exit code 1 (file not found)
      3. grep -c '\.btn-primary' styles.css → ≥ 1
      4. grep -c '\.hero-cinema' styles.css → ≥ 1
    Expected Result: Clean Ferrari CSS, no Lamborghini leftovers
    Evidence: .sisyphus/evidence/task-3-css-clean.txt
  ```

  **Commit**: YES (with Task 1, 2)

---

- [x] 4. **Navbar → Ferrari Top-Nav-on-Dark**

  **What to do**:
  - Riscrivere `frontend/CineBase.Web/wwwroot/components/navbar-landing.html`
  - Nuovo design Ferrari:
    - Altezza 64px (invece di 80px)
    - Background `var(--ferrari-canvas)` (#181818)
    - Testo bianco `var(--ferrari-ink)`
    - Border-bottom 1px `var(--ferrari-hairline)` (#303030)
    - Nav links: uppercase, tracking 0.65px, `var(--ferrari-nav-link)` (13px/600)
    - Logo CINEBASE al centro, tracking 4px uppercase
    - Icone a destra: search, user
    - Mobile hamburger a sinistra
    - REMPLAZZARE classi Tailwind personalizzate con classi Ferrari:
      - `bg-charcoal` → `bg-canvas` (o bg `var(--ferrari-canvas)`)
      - `text-gold` → `text-ferrari-primary`
      - `hover:text-gold` → `hover:text-ferrari-primary`
      - `border-white/10` → mantenere (è Ferrari-hairline)
      - Rimuovere rounded su qualsiasi elemento
    - Mobile menu: full-height, stesso dark background, links uppercase grandi

  **Must NOT do**:
  - Non usare gold (#D4AF37) da nessuna parte
  - Non usare rounded corners sul menu

  **Recommended Agent Profile**:
  - Category: `visual-engineering`
  - Skills: `[]`
  - Reason: UI component with visual design precision

  **Parallelization**:
  - Can Run In Parallel: YES with Task 5
  - Blocks: Tasks 6-17 (navbar è caricato ovunque)
  - Blocked By: Task 1, 3

  **References**:
  - `frontend/CineBase.Web/wwwroot/components/navbar-landing.html` — Current navbar
  - `DESIGN.md:139-147` — top-nav-on-dark component spec
  - `DESIGN.md:104-109` — nav-link typography
  - `DESIGN.md:498-511` — Responsive behavior (mobile hamburger below 768px)

  **Acceptance Criteria**:
  - [ ] Navbar height 64px
  - [ ] Nav links uppercase con tracking
  - [ ] Nessun colore gold
  - [ ] Mobile menu funzionante

  **QA Scenarios**:
  ```
  Scenario: Navbar renders correctly
    Tool: Playwright
    Preconditions: Dev server running, navbar loaded on index.html
    Steps:
      1. Navigate to http://localhost:5000/index.html
      2. Check navbar height is 64px → computed style
      3. Check nav link text-transform uppercase
      4. Check background is #181818
      5. Take screenshot
    Expected Result: Ferrari-style dark navbar with uppercase links
    Evidence: .sisyphus/evidence/task-4-navbar.png
  ```

  **Commit**: YES (with Task 5)

---

- [x] 5. **Footer → Ferrari Footer-dark**

  **What to do**:
  - Riscrivere `frontend/CineBase.Web/wwwroot/components/footer-landing.html`
  - Nuovo design Ferrari:
    - Background `var(--ferrari-canvas)` (#181818)
    - Testo `var(--ferrari-body)` (#969696) per links
    - Padding 64px 48px
    - 5 colonne a desktop (Brand, Navigazione, Supporto, Social, Legale)
    - Footer links: `var(--ferrari-body-sm)` (13px/400)
    - REMPLAZZARE:
      - `bg-brand-surface-container` → bg-canvas
      - `text-brand-gold` → `text-ferrari-primary`
      - `text-brand-on-surface` → `text-ink`
      - `text-brand-on-surface-variant` → `text-body`
      - `hover:text-brand-gold` → `hover:text-ferrari-primary`
      - `bg-brand-surface-container-highest` → bg-canvas-elevated
      - `border-brand-outline-variant/10` → `border-hairline`
    - Rimuovere rounded-full sui social icon (usare square)
  - Riscrivere `frontend/CineBase.Web/wwwroot/components/footer-admin.html` con stesso stile

  **Must NOT do**:
  - Non usare gold
  - Non usare rounded

  **Recommended Agent Profile**:
  - Category: `visual-engineering`
  - Skills: `[]`

  **Parallelization**:
  - Can Run In Parallel: YES with Task 4
  - Blocks: Tasks 6-17
  - Blocked By: Task 1, 3

  **References**:
  - `frontend/CineBase.Web/wwwroot/components/footer-landing.html` — Current footer
  - `frontend/CineBase.Web/wwwroot/components/footer-admin.html` — Admin footer
  - `DESIGN.md:261-265` — footer-dark spec
  - `DESIGN.md:266-269` — footer-link spec

  **QA Scenarios**:
  ```
  Scenario: Footer renders correctly
    Tool: Playwright
    Preconditions: Dev server running
    Steps:
      1. Navigate to http://localhost:5000/index.html
      2. Scroll to footer
      3. Check background is #181818
      4. Check link color is #969696
      5. Take screenshot
    Expected Result: Ferrari-style dark footer
    Evidence: .sisyphus/evidence/task-5-footer.png
  ```

  **Commit**: YES (with Task 4)

---

- [x] 6. **index.html — Full-bleed Cinematic Hero + Featured Section**

  **What to do**:
  - Riscrivere la sezione Hero di `frontend/CineBase.Web/wwwroot/index.html`
  - Nuovo design Ferrari full-bleed:
    - Hero image full-bleed (viewport-width, copre tutta la sezione)
    - Overlay scuro gradiente dal basso
    - Titolo h1 in basso a sinistra: `ferrari-display-mega` (80px/500/-1.6px) o responsive
    - Sottotitolo sotto il titolo
    - Opzionale: CTA Rosso Corsa + Outline CTA sotto il testo
    - Padding: 0 (full-bleed)
  - Riscrivere la sezione "In Evidenza Questa Settimana":
    - REMPLAZZARE classi:
      - `bg-brand-surface-container-low` → `bg-canvas`
      - `text-brand-on-surface` → `text-ink`
      - `text-brand-on-surface-variant` → `text-body`
      - `text-brand-gold` → `text-ferrari-primary`
      - `bg-brand-gold` → `bg-ferrari-primary` (per la barra decorativa)
      - `btn-outline-brand` → `btn-outline`
      - Rimuovere `rounded` sulla barra decorativa
    - Featured grid: card-ferrari invece di card-elevated
  - REMPLAZZARE classi del body:
    - `bg-brand-surface text-brand-on-surface font-sans` → `bg-canvas text-ink font-sans`
  - REMPLAZZARE title tag

  **Must NOT do**:
  - Non usare rounded corners
  - Non usare gold
  - Non mantenere classi Lamborghini

  **Recommended Agent Profile**:
  - Category: `visual-engineering`
  - Skills: `[]`
  - Reason: Hero section is the most visually impactful page

  **Parallelization**:
  - Can Run In Parallel: YES with Task 7
  - Blocks: None
  - Blocked By: Task 1, 3, 4, 5

  **References**:
  - `frontend/CineBase.Web/wwwroot/index.html` — Current homepage
  - `DESIGN.md:275-278` — Cinema hero description
  - `DESIGN.md:177-178` — hero-band-cinema spec
  - `DESIGN.md:35-39` — display-mega typography

  **QA Scenarios**:
  ```
  Scenario: Hero full-bleed with Ferrari styling
    Tool: Playwright
    Preconditions: Dev server running
    Steps:
      1. Navigate to http://localhost:5000/index.html
      2. Check hero image is full width (no padding left/right)
      3. Check title uses correct typography
      4. Check CTA buttons have sharp corners (border-radius: 0px)
      5. Check CTA button background is #da291c
      6. Take screenshot
    Expected Result: Ferrari-style full-bleed hero
    Evidence: .sisyphus/evidence/task-6-index-hero.png
  ```

  **Commit**: YES (with Task 7)

---

- [x] 7. **dashboard.html — Ferrari Admin Layout**

  **What to do**:
  - Riscrivere `frontend/CineBase.Web/wwwroot/dashboard.html`
  - REMPLAZZARE classi:
    - `bg-brand-surface` → `bg-canvas`
    - `text-brand-on-surface` → `text-ink`
    - `text-brand-on-surface-variant` → `text-body`
    - `text-brand-gold` → `text-ferrari-primary`
    - `bg-brand-gold` → `bg-ferrari-primary`
    - `bg-brand-cyan` → `bg-ferrari-primary` (icone stat box)
    - `bg-brand-emerald` → `bg-ferrari-primary`
    - `bg-brand-indigo-light` → `bg-ferrari-primary` (tutte le icone stat box → Rosso Corsa)
    - `btn-gold` → `btn-primary`
    - `btn-outline-brand` → `btn-outline`
    - `btn-ghost` → `btn-tertiary`
    - `card-elevated` → `card-ferrari`
    - Rimuovere `rounded-*` su tutti gli elementi
    - Rimuovere `rounded-xl`, `rounded-md`, `rounded-lg` sulle card e bottoni
  - Stat boxes: mantenere layout ma con icone rosse su sfondo scuro

  **Must NOT do**:
  - Non modificare JS (id, data attributes, onclick handlers)
  - Non modificare la struttura HTML (solo classi)

  **Recommended Agent Profile**:
  - Category: `visual-engineering`
  - Skills: `[]`

  **Parallelization**:
  - Can Run In Parallel: YES with Task 6
  - Blocks: None
  - Blocked By: Task 1, 3

  **References**:
  - `frontend/CineBase.Web/wwwroot/dashboard.html` — Current dashboard
  - `DESIGN.md:149-153` — button-primary spec
  - `DESIGN.md:160-164` — button-outline-on-dark spec

  **QA Scenarios**:
  ```
  Scenario: Dashboard Ferrari styling
    Tool: Playwright
    Steps:
      1. Navigate to http://localhost:5000/dashboard.html
      2. Check stat box icons are on red bg
      3. Check buttons have sharp corners
      4. Check card backgrounds are canvas-elevated
      5. Take screenshot
    Expected Result: Ferrari-styled admin dashboard
    Evidence: .sisyphus/evidence/task-7-dashboard.png
  ```

  **Commit**: YES (with Task 6)

---

- [x] 8. **films.html + scheda-film.html — Ferrari Content Cards**

  **What to do**:
  - Riscrivere classi in `frontend/CineBase.Web/wwwroot/films.html` e `frontend/CineBase.Web/wwwroot/scheda-film.html`
  - REMPLAZZARE:
    - `bg-brand-surface` → `bg-canvas`
    - `text-brand-on-surface` → `text-ink`
    - `text-brand-on-surface-variant` → `text-body`
    - `text-brand-gold` → `text-ferrari-primary`
    - `card-elevated` → `card-ferrari`
    - `btn-outline-brand` → `btn-outline`
    - `btn-gold-sm` → `btn-primary` (con padding ridotto)
    - `rounded-xl`, `rounded-lg` → rimuovere
    - `ghost-input` → `input-ferrari`
    - `bg-brand-surface-container` → `bg-canvas-elevated`
    - `chip-status`, `chip-active` → `badge-ferrari`
  - scheda-film: la cover image perde rounded-xl

  **Must NOT do**:
  - Non modificare JS (id degli elementi, data attributes)

  **Recommended Agent Profile**:
  - Category: `visual-engineering`
  - Skills: `[]`

  **Parallelization**:
  - Can Run In Parallel: YES with Tasks 9, 10, 11, 12
  - Blocks: None
  - Blocked By: Task 1, 3

  **References**:
  - `frontend/CineBase.Web/wwwroot/films.html` — Current films page
  - `frontend/CineBase.Web/wwwroot/scheda-film.html` — Current film detail page

  **QA Scenarios**:
  ```
  Scenario: Films page Ferrari styling
    Tool: Playwright
    Steps:
      1. Navigate to http://localhost:5000/films.html
      2. Check card has sharp corners
      3. Check text colors are Ferrari palette
      4. Take screenshot
    Expected Result: Ferrari-styled films listing
    Evidence: .sisyphus/evidence/task-8-films.png
  ```

  **Commit**: YES (with Tasks 9-12)

---

- [x] 9. **programmazione.html — Ferrari Schedule**

  **What to do**:
  - Riscrivere classi in `frontend/CineBase.Web/wwwroot/programmazione.html`
  - REMPLAZZARE:
    - `bg-brand-surface` → `bg-canvas`
    - `text-brand-on-surface` → `text-ink`
    - `text-brand-on-surface-variant` → `text-body`
    - `card-elevated` → `card-ferrari`
    - `btn-outline-brand` → `btn-outline`
    - `btn-gold` → `btn-primary`
    - `ghost-input` → `input-ferrari`
    - `bg-brand-surface-container` → `bg-canvas-elevated`
    - `bg-brand-surface-container-lowest` → `bg-canvas` (o #181818, dipende)
    - `bg-brand-gold` → `bg-ferrari-primary`
    - `text-gold` → `text-ferrari-primary`
    - Rimuovere tutti i rounded su tab-btn, date-rail, show-time buttons
    - Tab attivi: bg `var(--ferrari-primary)` invece di gold
    - Date rail: bordo `var(--ferrari-hairline)`, attivo bg `var(--ferrari-primary)`
    - Show time buttons: bordo hairline, hover Rosso Corsa

  **Must NOT do**:
  - Non modificare JS o data attributes

  **Recommended Agent Profile**:
  - Category: `visual-engineering`
  - Skills: `[]`

  **Parallelization**:
  - Can Run In Parallel: YES with Tasks 8, 10, 11, 12
  - Blocked By: Task 1, 3

  **References**:
  - `frontend/CineBase.Web/wwwroot/programmazione.html` — Current schedule page
  - `styles.css` date-rail, show-time, tab classes (da Task 3)

  **QA Scenarios**:
  ```
  Scenario: Schedule page Ferrari styling
    Tool: Playwright
    Steps:
      1. Navigate to http://localhost:5000/programmazione.html
      2. Check tabs have sharp corners
      3. Check date rail buttons
      4. Take screenshot
    Expected Result: Ferrari-styled schedule
    Evidence: .sisyphus/evidence/task-9-programmazione.png
  ```

  **Commit**: YES (with Tasks 8, 10-12)

---

- [x] 10. **proiezioni.html — Ferrari Projections**

  **What to do**:
  - Riscrivere classi in `frontend/CineBase.Web/wwwroot/proiezioni.html`
  - Stesse sostituzioni di Task 9 (bread-and-butter class replacement)
  - Tabella proiezioni: rimuovere rounded su header/cells
  - Bottoni azione: `btn-outline-brand` → `btn-outline`

  **Recommended Agent Profile**:
  - Category: `visual-engineering`
  - Skills: `[]`

  **Parallelization**:
  - Can Run In Parallel: YES with Tasks 8, 9, 11, 12
  - Blocked By: Task 1, 3

  **QA Scenarios**:
  ```
  Scenario: Projections page Ferrari styling
    Tool: Playwright
    Steps:
      1. Navigate to http://localhost:5000/proiezioni.html
      2. Check table styling
      3. Check buttons
      4. Take screenshot
    Expected Result: Ferrari-styled projections
    Evidence: .sisyphus/evidence/task-10-proiezioni.png
  ```

  **Commit**: YES (with Tasks 8, 9, 11, 12)

---

- [x] 11. **categorie.html + registi.html — Ferrari Listing Pages**

  **What to do**:
  - Riscrivere classi in `frontend/CineBase.Web/wwwroot/categorie.html` e `frontend/CineBase.Web/wwwroot/registi.html`
  - Stesso pattern di sostituzione classi (bg-canvas, text-ink, text-body, text-ferrari-primary, card-ferrari, btn-primary, btn-outline, input-ferrari)

  **Recommended Agent Profile**:
  - Category: `visual-engineering`
  - Skills: `[]`

  **Parallelization**:
  - Can Run In Parallel: YES with Tasks 8, 9, 10, 12
  - Blocked By: Task 1, 3

  **QA Scenarios**:
  ```
  Scenario: Categories/Directors pages Ferrari styling
    Tool: Playwright
    Steps:
      1. Navigate to http://localhost:5000/categorie.html
      2. Take screenshot
      3. Navigate to http://localhost:5000/registi.html
      4. Take screenshot
    Expected Result: Ferrari-styled listing pages
    Evidence: .sisyphus/evidence/task-11-categorie.png, task-11-registi.png
  ```

  **Commit**: YES (with Tasks 8, 9, 10, 12)

---

- [x] 12. **cinemas.html + my-cinemas.html — Ferrari Cinema Pages**

  **What to do**:
  - Riscrivere classi in `frontend/CineBase.Web/wwwroot/cinemas.html` e `frontend/CineBase.Web/wwwroot/my-cinemas.html`
  - Pattern sostituzione classi standard
  - Film schedule card (my-cinemas): rimuovere rounded, gold hover → Rosso Corsa hover
  - Cinema cards: `card-elevated` → `card-ferrari`, no rounded

  **Recommended Agent Profile**:
  - Category: `visual-engineering`
  - Skills: `[]`

  **Parallelization**:
  - Can Run In Parallel: YES with Tasks 8, 9, 10, 11
  - Blocked By: Task 1, 3

  **QA Scenarios**:
  ```
  Scenario: Cinema pages Ferrari styling
    Tool: Playwright
    Steps:
      1. Navigate to http://localhost:5000/cinemas.html
      2. Take screenshot
      3. Navigate to http://localhost:5000/my-cinemas.html
      4. Take screenshot
    Expected Result: Ferrari-styled cinema pages
    Evidence: .sisyphus/evidence/task-12-cinemas.png, task-12-my-cinemas.png
  ```

  **Commit**: YES (with Tasks 8-11)

---

- [x] 13. **login.html + registrazione.html — Ferrari Auth Pages**

  **What to do**:
  - Riscrivere classi in `frontend/CineBase.Web/wwwroot/login.html` e `frontend/CineBase.Web/wwwroot/registrazione.html`
  - REMPLAZZARE:
    - `bg-brand-surface` → `bg-canvas`
    - `text-brand-surface` → bg-canvas (sulla card)
    - `glass-panel card-elevated` → `card-ferrari` (solo card-ferrari, niente glass)
    - `rounded-2xl` → rimuovere (0px)
    - `rounded-xl` → rimuovere
    - `ghost-input` → `input-ferrari`
    - `btn-gold` → `btn-primary`
    - `text-brand-gold` → `text-ferrari-primary`
    - `text-brand-on-surface` → `text-ink`
    - `text-brand-on-surface-variant` → `text-body`
    - `bg-brand-error-container` → `bg-ferrari-primary` (opacizzato) o gestire semantic
    - `border-brand-error/30` → border Rosso Corsa
    - `text-brand-error` → `#f13a2c` (semantic-warning da DESIGN.md)

  **Must NOT do**:
  - Non modificare form structure, id, name attributes

  **Recommended Agent Profile**:
  - Category: `visual-engineering`
  - Skills: `[]`

  **Parallelization**:
  - Can Run In Parallel: YES with Tasks 14, 15, 16, 17
  - Blocked By: Task 1, 3

  **References**:
  - `frontend/CineBase.Web/wwwroot/login.html` — Current login
  - `frontend/CineBase.Web/wwwroot/registrazione.html` — Current registration
  - `DESIGN.md:231-237` — text-input-on-dark spec

  **QA Scenarios**:
  ```
  Scenario: Login page Ferrari styling
    Tool: Playwright
    Steps:
      1. Navigate to http://localhost:5000/login.html
      2. Check card has NO rounded corners (border-radius: 0)
      3. Check input has border-radius 4px
      4. Check button has sharp corners, bg #da291c
      5. Take screenshot
      6. Navigate to http://localhost:5000/registrazione.html
      7. Take screenshot
    Expected Result: Ferrari-styled auth pages
    Evidence: .sisyphus/evidence/task-13-login.png, task-13-registrazione.png
  ```

  **Commit**: YES (with Tasks 14-17)

---

- [x] 14. **acquista.html — Ferrari Seat Map**

  **What to do**:
  - Riscrivere classi in `frontend/CineBase.Web/wwwroot/acquista.html`
  - REMPLAZZARE:
    - `bg-brand-surface` → `bg-canvas`
    - `text-brand-on-surface` → `text-ink`
    - `text-brand-on-surface-variant` → `text-body`
    - `text-brand-gold` → `text-ferrari-primary`
    - `card-elevated` → `card-ferrari`
    - `rounded-xl` → rimuovere
    - `bg-brand-surface-container` → `bg-canvas-elevated`
    - `seat-map-screen`: gold gradient → Rosso Corsa gradient (o meglio solid Rosso Corsa)
    - `seat-selected`: gold → `var(--ferrari-primary)`
    - `seat-available:hover`: gold border → `var(--ferrari-primary)` border
    - `btn-outline-brand` → `btn-outline`

  **Must NOT do**:
  - Non modificare JS seat selection logic

  **Recommended Agent Profile**:
  - Category: `visual-engineering`
  - Skills: `[]`

  **Parallelization**:
  - Can Run In Parallel: YES with Tasks 13, 15, 16, 17
  - Blocked By: Task 1, 3

  **References**:
  - `frontend/CineBase.Web/wwwroot/acquista.html` — Current seat map page
  - `styles.css` seat-map classes (da Task 3)

  **QA Scenarios**:
  ```
  Scenario: Seat map Ferrari styling
    Tool: Playwright
    Steps:
      1. Navigate to http://localhost:5000/acquista.html
      2. Check screen indicator is Rosso Corsa
      3. Check seat legend colors updated
      4. Take screenshot
    Expected Result: Ferrari-styled seat map
    Evidence: .sisyphus/evidence/task-14-acquista.png
  ```

  **Commit**: YES (with Tasks 13, 15-17)

---

- [x] 15. **pagamento.html + esito-acquisto.html — Ferrari Payment Pages**

  **What to do**:
  - Riscrivere classi in `frontend/CineBase.Web/wwwroot/pagamento.html` e `frontend/CineBase.Web/wwwroot/esito-acquisto.html`
  - REMPLAZZARE:
    - `bg-brand-surface` → `bg-canvas`
    - `text-brand-on-surface` → `text-ink`
    - `text-brand-on-surface-variant` → `text-body`
    - `text-brand-gold` → `text-ferrari-primary`
    - `card-elevated` → `card-ferrari`
    - `rounded-xl`, `rounded-lg` → rimuovere
    - `btn-gold` → `btn-primary`
    - `btn-outline-brand` → `btn-outline`
    - `payment-option-card`: gold border checked → Rosso Corsa border checked
    - `bg-brand-surface-container-lowest` → bg-canvas (dipende dal contesto)
    - `bg-brand-surface-container` → `bg-canvas-elevated`
    - `ghost-input` → `input-ferrari`
    - Slider (range input): gold thumb → Rosso Corsa thumb

  **Recommended Agent Profile**:
  - Category: `visual-engineering`
  - Skills: `[]`

  **Parallelization**:
  - Can Run In Parallel: YES with Tasks 13, 14, 16, 17
  - Blocked By: Task 1, 3

  **QA Scenarios**:
  ```
  Scenario: Payment page Ferrari styling
    Tool: Playwright
    Steps:
      1. Navigate to http://localhost:5000/pagamento.html
      2. Check payment cards have no rounded corners
      3. Check CTA buttons sharp, Rosso Corsa
      4. Take screenshot
      5. Navigate to http://localhost:5000/esito-acquisto.html
      6. Take screenshot
    Expected Result: Ferrari-styled payment pages
    Evidence: .sisyphus/evidence/task-15-pagamento.png, task-15-esito.png
  ```

  **Commit**: YES (with Tasks 13, 14, 16, 17)

---

- [x] 16. **profilo.html — Ferrari Profile Page**

  **What to do**:
  - Riscrivere classi in `frontend/CineBase.Web/wwwroot/profilo.html`
  - Pattern sostituzione standard:
    - `bg-brand-surface` → `bg-canvas`
    - `text-brand-on-surface` → `text-ink`
    - `text-brand-on-surface-variant` → `text-body`
    - `text-brand-gold` → `text-ferrari-primary`
    - `card-elevated` → `card-ferrari`
    - `rounded-xl`, `rounded-lg`, `rounded-md` → rimuovere
    - `btn-gold` → `btn-primary`
    - `btn-outline-brand` → `btn-outline`
    - `ghost-input` → `input-ferrari`
    - `bg-brand-surface-container` → `bg-canvas-elevated`
    - Top-up amount buttons: selected state gold → Rosso Corsa
    - Badge/pill: gold active → Rosso Corsa active

  **Recommended Agent Profile**:
  - Category: `visual-engineering`
  - Skills: `[]`

  **Parallelization**:
  - Can Run In Parallel: YES with Tasks 13, 14, 15, 17
  - Blocked By: Task 1, 3

  **QA Scenarios**:
  ```
  Scenario: Profile page Ferrari styling
    Tool: Playwright
    Steps:
      1. Navigate to http://localhost:5000/profilo.html
      2. Check profile cards styling
      3. Check buttons
      4. Take screenshot
    Expected Result: Ferrari-styled profile
    Evidence: .sisyphus/evidence/task-16-profilo.png
  ```

  **Commit**: YES (with Tasks 13-15, 17)

---

- [x] 17. **tmdb-search.html — Ferrari TMDB Search**

  **What to do**:
  - Riscrivere classi in `frontend/CineBase.Web/wwwroot/tmdb-search.html`
  - Stesso pattern sostituzione standard
  - Search results cards: rounded → 0, gold hover → Rosso Corsa hover
  - Input search: `ghost-input` → `input-ferrari`

  **Recommended Agent Profile**:
  - Category: `visual-engineering`
  - Skills: `[]`

  **Parallelization**:
  - Can Run In Parallel: YES with Tasks 13, 14, 15, 16
  - Blocked By: Task 1, 3

  **QA Scenarios**:
  ```
  Scenario: TMDB search Ferrari styling
    Tool: Playwright
    Steps:
      1. Navigate to http://localhost:5000/tmdb-search.html
      2. Check search input styling
      3. Check result cards (if any rendered)
      4. Take screenshot
    Expected Result: Ferrari-styled search
    Evidence: .sisyphus/evidence/task-17-tmdb-search.png
  ```

  **Commit**: YES (with Tasks 13-16)

---

## Final Verification Wave (MANDATORY — after ALL implementation tasks)

- [x] F1. **Plan Compliance Audit** — `oracle`
  Read the plan end-to-end. For each "Must Have": verify implementation exists (read files, inspect CSS). For each "Must NOT Have": search codebase for forbidden patterns (gold colors, rounded corners on CTAs, shadows, glassmorphism). Check evidence files exist in .sisyphus/evidence/. Compare deliverables against plan.
  Output: `Must Have [N/N] | Must NOT Have [N/N] | Tasks [N/N] | VERDICT`

- [x] F2. **Full QA Screenshot Review** — `unspecified-high` (+ Playwright)
  Open each of the 17 HTML pages + 3 component templates loaded via navbar/footer (run dev server first). Take full-page screenshots. Verify each against Ferrari design spec. Check: colors correct, no stray gold/rounded/shadow, typography correct, layout renders properly.
  Output: `Pages [N/N verified] | Issues [N] | Screenshots in .sisyphus/evidence/final-qa/`

- [x] F3. **Scope Fidelity Check** — `deep`
  For each task: read "What to do", read actual diff (git log/diff). Verify 1:1 — everything in spec was built, nothing beyond spec was built. Check "Must NOT do" compliance. Detect JS file contamination.
  Output: `Tasks [N/N compliant] | Scope creep [CLEAN/N issues] | VERDICT`

---

## Commit Strategy

- Task 1-3: `feat(css): Ferrari design system tokens + styles.css rewrite`
- Task 4-5: `feat(components): Ferrari navbar and footer redesign`
- Task 6-7: `feat(pages): Ferrari index hero + dashboard`
- Task 8-12: `feat(pages): Ferrari content pages (films, schedule, projections, categories)`
- Task 13-17: `feat(pages): Ferrari customer pages (login, booking, payment, profile)`
- Task F1-F3: `chore(qa): Ferrari design verification and compliance`

---

## Success Criteria

- Nessuna classe `brand-gold`, `brand-indigo` nei file HTML
- Nessun `rounded-*` su bottoni o card (solo badge, input)
- Tutti i bottoni primari: bg Rosso Corsa, sharp corners, uppercase tracking
- Navbar: 64px, dark, uppercase nav links
- Hero index: full-bleed, titolo fluttuante
- Canvas: #181818 su tutte le pagine
- cyber.css: eliminato
- styles.css: riscritto con Ferrari tokens
- Nessun errore console nelle pagine
- Tutti gli screenshot generati e verificati
