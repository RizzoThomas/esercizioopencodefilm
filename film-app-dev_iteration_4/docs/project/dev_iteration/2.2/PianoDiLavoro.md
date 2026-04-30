# Piano di Lavoro - Iterazione 2.2: UI Rework "Cinema Graphite"

## 1) Obiettivo

Rework completo della UI del frontend CineBase basato sul design system **"Cinema Graphite"** generato nel progetto Stitch "Modern CineBase Style" (ID: `3151130682396165519`).

**Principi guida:**
- **Nessuna modifica funzionale** alle logiche JS esistenti (API calls, CRUD, paginazione, filtri, upload)
- Modifiche ai file JS solo per adattare classi CSS e selettori se necessario
- Tema **LIGHT + DARK** con toggle (default: system preference `prefers-color-scheme`)
- Filosofia design: *"The Digital Curator"* — profondità tonale, glassmorphism, tipografia editoriale, nessun bordo rigido
- Tema chiaro: colori dal design system Stitch "Cinema Graphite" (gold, indigo, surface tinted)
- Tema scuro: palette derivata dagli screen dark mode del progetto Stitch (deep indigo, gold accent)

---

## 2) Riferimenti Design (Stitch)

### 2.1 Design System: "Cinema Graphite"

**Token estratti dal design system Stitch:**

| Token | Valore | Note |
|-------|--------|------|
| `colorMode` | LIGHT | Tema unico per tutte le pagine |
| `customColor` (Primary) | `#D4AF37` | Oro/Ambra — azioni primarie, CTA |
| `primary` (resolved) | `#735c00` | Container primary |
| `secondary` | `#5c5c73` | Indigo scuro |
| `tertiary` | `#006a6a` / `#10B981` | Cyan/Emerald per indicatori stato |
| `headlineFont` | INTER | Titoli e heading |
| `bodyFont` | INTER | Corpo testo |
| `roundness` | ROUND_EIGHT | Border-radius 8px (0.5rem) |
| `surface` | `#fbf8ff` | Background principale |
| `surface_container` | `#eeecff` | Card/pannelli secondari |
| `surface_container_lowest` | `#ffffff` | Card su superfici piu chiare |
| `on_surface` | `#191a2d` | Testo principale |
| `outline_variant` | `#d0c5af` | Bordi fantasma (15% opacity) |

### 2.2 Filosofia Design: "The Digital Curator"

Regole chiave dal DESIGN.md Stitch:

1. **Regola "No-Line"**: Vietati bordi 1px solid per sezioni. Usare shift di colore background
2. **Surface Hierarchy & Nesting**: `surface_container_highest` dentro `surface_container` per profondita naturale
3. **Glassmorphism**: Sidebar e floating panels con gradient + `backdrop-blur(20px)` a 85% opacity
4. **Tonal Layering**: Cards `surface_container_lowest` su background `surface_container_low`
5. **Ambient Shadows**: Ombre a 4 livelli con tinta `on_surface` (`0px 10px 30px rgba(25, 26, 45, 0.06)`)
6. **Ghost Border**: Solo `outline_variant` al 15% opacity per input. Mai bordi opachi al 100%
7. **Typography Labels**: All-Caps, letter-spacing `0.05em`, font-size 0.75rem
8. **Negative Space**: Generoso padding per respiro visivo

### 2.3 Implementazione Dual Theme

**Approccio: CSS Custom Properties + Tailwind CDN**

Tutti i brand colori sono definiti come CSS custom properties (`--brand-*`) in `css/styles.css`:
- `:root { ... }` = valori tema chiaro (Cinema Graphite light)
- `.dark { ... }` = valori tema scuro (deep indigo + gold accent)

Il `tailwind.config` in ogni pagina mappa le variabili CSS come colori brand:
```javascript
colors: {
  brand: {
    gold: 'var(--brand-gold)',
    surface: 'var(--brand-surface)',
    'on-surface': 'var(--brand-on-surface)',
    // ...
  }
}
```

Quando l'utente cambia tema (o il system preference cambia), viene aggiunta/rimossa la classe `dark` su `<html>`. Le CSS variables cambiano valori e TUTTI i componenti si aggiornano automaticamente.

**Toggle**: bottone con icone moon/sun nelle navbar. Persistenza via `localStorage` key `cinebase-theme`. Fallback a `prefers-color-scheme`.

**File**: `js/theme.js` — IIFE che espone `window.CineBaseTheme` (toggle, set, get, getSystem).

### 2.4 Schermate Stitch di Riferimento

Screens disponibili nel progetto Stitch:

| Screen ID | Titolo | Pagina corrispondente |
|-----------|--------|-----------------------|
| `9c53e47e41df4334816f40d7d5aa9799` | CineBase Cinema Landing Page | index.html |
| `3d7b7611a2844b5bb49821119323b031` | Unified Admin Dashboard | dashboard.html |
| `141f766e2cd04e95a3b0c536419f97c2` | Film Management Workspace | films.html |
| `f222a4857f7e407faf51b26892285289` | CineBase Visual System Design | Component library (light) |
| `e4fb8b16635d4f4a9f325e0a7d4c0600` | CineBase Dark Visual System Design | Component library (dark) |
| `7145b92052ee44f897e2efef31765ee5` | CineBase UI Component Library (Dark Mode) | UI kit dark |
| `f42cb23501454628964362cfdd4dad57` | CineBase UI Component Library (Refined Nav) | UI kit con nav |

---

## 3) Struttura Attuale del Frontend

```
frontend/CineBase.Web/wwwroot/
├── index.html          ← Landing page (dark theme) → diventa LIGHT
├── dashboard.html      ← Dashboard admin (light, slate/indigo) → Cinema Graphite
├── films.html          ← CRUD Film → Cinema Graphite
├── registi.html        ← CRUD Registi → Cinema Graphite
├── cinemas.html        ← CRUD Cinema → Cinema Graphite
├── proiezioni.html     ← CRUD Proiezioni → Cinema Graphite
├── components/
│   ├── navbar-landing.html   ← Dark navbar → unificare
│   ├── navbar-admin.html     ← Light navbar → Cinema Graphite
│   ├── footer-landing.html   ← Dark footer → unificare
│   └── footer-admin.html     ← Light footer → Cinema Graphite
├── css/
│   └── styles.css      ← Custom styles → riscrivere
├── js/
│   ├── theme.js        ← NUOVO: gestione tema light/dark (localStorage + system pref)
│   ├── api.js          ← API client → NESSUNA modifica
│   ├── utils.js        ← Utility → aggiornare colori toast se serve
│   ├── template-loader.js  ← NESSUNA modifica
│   ├── navbar.js       ← Aggiornare classi CSS attivi
│   ├── form-handlers.js    ← NESSUNA modifica
│   └── pages/
│       ├── home.js     ← NESSUNA modifica funzionale
│       ├── films.js    ← NESSUNA modifica funzionale
│       ├── registi.js  ← NESSUNA modifica funzionale
│       ├── cinemas.js  ← NESSUNA modifica funzionale
│       └── proiezioni.js   ← NESSUNA modifica funzionale
```

---

## 4) Piano di Modifiche per File

### 4.1 Tailwind Config (in ogni pagina HTML)

**Attuale:**
```javascript
tailwind.config = {
  theme: {
    extend: {
      colors: {
        brand: {
          orange: "#FF8C00",
          "orange-dark": "#E67E00",
          dark: "#121212",
          "dark-lighter": "#1E1E1E",
          "dark-card": "#2A2A2A"
        }
      }
    }
  }
}
```

**Nuovo (Cinema Graphite):**
```javascript
tailwind.config = {
  theme: {
    extend: {
      colors: {
        brand: {
          gold: "#D4AF37",
          "gold-dark": "#735c00",
          "gold-light": "#ffe088",
          indigo: "#1A1B2E",
          "indigo-light": "#5c5c73",
          cyan: "#006a6a",
          "cyan-light": "#00c7c7",
          emerald: "#10B981",
          surface: "#fbf8ff",
          "surface-dim": "#d9d8f2",
          "surface-container": "#eeecff",
          "surface-container-high": "#e7e6ff",
          "surface-container-highest": "#e1e0fb",
          "surface-container-low": "#f5f2ff",
          "surface-container-lowest": "#ffffff",
          "on-surface": "#191a2d",
          "on-surface-variant": "#4d4635",
          outline: "#7f7663",
          "outline-variant": "#d0c5af",
          error: "#ba1a1a",
          "error-container": "#ffdad6"
        }
      }
    }
  }
}
```

I colori `brand.dark`, `brand.dark-lighter`, `brand.dark-card` vengono RIMOSSI.

### 4.2 css/styles.css — Riscrittura Completa

Mantenere le utility esistenti utili (scrollbar, animations, modal-backdrop) e aggiungere:

- `glass-panel`: gradient + backdrop-blur(20px) per pannelli flottanti
- `ambient-shadow`: `0px 10px 30px rgba(25, 26, 45, 0.06)`
- `ghost-input`: background surface_container_highest + border outline_variant 15% + gold left-accent al focus
- `label-caps`: font-size 0.75rem, font-weight 600, text-transform uppercase, letter-spacing 0.05em
- `sidebar-glass`: gradient indigo dark (#1A1B2E → #2e2f43) + backdrop-blur
- `card-elevated`: white bg + ambient-shadow + border-radius 0.5rem
- `card-container`: surface_container bg + border-radius 0.5rem
- `hero-overlay-light`: gradient da surface (non piu dark)
- `btn-gold`: gradient gold (#D4AF37 → #735c00), white text, rounded 0.5rem
- `chip-status` / `chip-active` / `chip-past`: pill-shaped status indicators con colori brand
- Mantenere: modal-backdrop, fadeIn, card-hover, scrollbar, print styles, responsive images

### 4.3 components/navbar-landing.html — Light Theme

**Modifiche chiave:**
- Background: da `bg-brand-dark/95 backdrop-blur-md border-b border-white/10` a glass panel light con ambient-shadow
- Logo: da `text-brand-orange` a `text-brand-gold`
- Links: da `text-white` a `text-brand-on-surface`
- CTA "Area Admin": da `bg-brand-orange hover:bg-brand-orange-dark shadow-brand-orange/20` a `btn-gold`
- Mobile menu: da `bg-brand-dark-lighter border-t border-white/10` a `bg-brand-surface-container`
- Mobile links: da `text-white hover:text-brand-orange` a `text-brand-on-surface hover:text-brand-gold`

### 4.4 components/navbar-admin.html — Cinema Graphite

**Modifiche chiave:**
- Navbar bg: da `bg-white border-b border-slate-200` a glass panel con ambient-shadow (no border)
- Logo icon: da `bg-indigo-600` a `bg-brand-gold` (o gradient gold)
- Nav links: hover da `hover:text-indigo-600` a `hover:text-brand-gold`
- Notification icon: colori brand
- Avatar: da `bg-indigo-100 text-indigo-700` a `bg-brand-surface-container-highest text-brand-on-surface`
- Login button: da `bg-indigo-600 hover:bg-indigo-700` a `btn-gold`
- Mobile: aggiornare colori coerentemente

### 4.5 components/footer-landing.html — Light Theme

**Modifiche chiave:**
- Background: da `bg-brand-dark-lighter border-t border-white/10` a `bg-brand-surface-container`
- Testo: da `text-gray-400` a `text-brand-on-surface-variant`
- Titoli: da `text-white` a `text-brand-on-surface`
- Logo: da `text-brand-orange` a `text-brand-gold`
- Hover links: da `hover:text-brand-orange` a `hover:text-brand-gold`
- Social icons: da `bg-white/5 hover:bg-brand-orange` a `bg-brand-surface-container-highest hover:bg-brand-gold`
- Copyright: da `text-gray-500` a `text-brand-on-surface-variant`

### 4.6 components/footer-admin.html — Cinema Graphite

**Modifiche chiave:**
- Background: da `border-t border-slate-200` a `bg-brand-surface-container-low` (no border)
- Testo: da `text-slate-400` a `text-brand-on-surface-variant`
- Hover: da `hover:text-indigo-600` a `hover:text-brand-gold`

### 4.7 index.html — Landing Page (Dark → Light)

**Modifiche chiave:**
- `<body>`: da `bg-brand-dark text-white` a `bg-brand-surface text-brand-on-surface font-sans`
- Hero section:
  - Overlay: da dark gradient a `hero-overlay-light`
  - Titolo: `text-brand-on-surface`, span gold: `text-brand-gold`
  - Sottotitolo: `text-brand-on-surface-variant`
  - CTA primario: `btn-gold`
  - CTA secondario: `bg-brand-surface-container border border-brand-outline-variant`
- Sezione "Film in Programmazione":
  - Background: da `bg-brand-dark-lighter` a `bg-brand-surface-container-low`
  - Titolo: `text-brand-on-surface`, linea: `bg-brand-gold`
  - Filtri: `ghost-input` style (non piu dark selects)
  - Film cards: da `bg-brand-dark-card border-white/10` a `card-elevated`
  - Card text: `text-brand-on-surface`, subtitle: `text-brand-on-surface-variant`
  - Genre chip: da `bg-brand-orange` a `bg-brand-gold`
  - Prenota button: `btn-gold`
  - Image overlay: da `from-black/80` a `from-brand-on-surface/30`
- Rimuovere Tailwind config vecchio colori, usare nuovo config

### 4.8 dashboard.html

**Modifiche chiave:**
- Sidebar:
  - Da `bg-slate-900` a `sidebar-glass` (indigo gradient + backdrop-blur)
  - Active link: da `bg-indigo-50 text-indigo-600` a `bg-white/10 text-brand-gold`
  - Hover links: mantenere `hover:bg-slate-800 hover:text-white` (funziona su indigo dark)
  - Settings link: stesso pattern hover
- Header: da `bg-white border-b border-slate-200` a glass panel con ambient-shadow
- Bottone header: da `bg-indigo-600` a `btn-gold`
- Stats cards: `card-elevated` con icone:
  - Film: `bg-brand-gold` (era `bg-indigo-500`)
  - Registi: `bg-brand-cyan` (era `bg-emerald-500`)
  - Cinema: `bg-brand-emerald` (era `bg-amber-500`)
  - Proiezioni: `bg-brand-indigo-light` (era `bg-purple-500`)
- Proiezioni table:
  - Header: da `bg-gray-50` a `bg-brand-surface-container`
  - Labels: `label-caps` style
  - Rows: da `divide-y divide-gray-200` a no-divide, `hover:bg-brand-surface-container-highest`
  - Status badge: `chip-active` / `chip-past`
- Section cards (Registi Evidenza, Analisi Cinema):
  - Da `border-b border-gray-200` a ambient-shadow header
  - Body: `bg-brand-surface-container-lowest`
- Modal proiezione:
  - Inputs: `ghost-input` style
  - Buttons: `btn-gold` primary, outline secondary

### 4.9 films.html

**Modifiche chiave:**
- Page header: testo `text-brand-on-surface`, subtitle `text-brand-on-surface-variant`
- Bottone Export: `bg-brand-surface-container border border-brand-outline-variant`
- Bottone Aggiungi Film: `btn-gold`
- Stats grid: `card-elevated` con icone brand (gold, cyan, emerald, indigo-light)
- Search/filtri container: `bg-brand-surface-container-lowest ambient-shadow` (no border)
- Input search: `ghost-input`
- Select filtri: `ghost-input`
- Table:
  - Container: `card-elevated` (no border)
  - Header: `bg-brand-surface-container`, labels `label-caps`
  - Rows: no divide, `hover:bg-brand-surface-container-highest`
  - Text: `text-brand-on-surface` / `text-brand-on-surface-variant`
- Pagination:
  - Container: `bg-brand-surface-container-low` (no border)
  - Buttons: `border-brand-outline-variant hover:bg-brand-surface-container-highest`
- Modal:
  - Header: `text-brand-on-surface`
  - Inputs: `ghost-input` con gold focus
  - Primary button: `btn-gold`
  - Cancel button: `bg-brand-surface-container border border-brand-outline-variant`

### 4.10 registi.html

**Modifiche chiave:** Stesse pattern di films.html:
- Stats cards: `card-elevated` con icone brand colors
- Table: tonal surfaces, no borders, `label-caps` headers
- Badge nazionalita: da `bg-blue-100 text-blue-800` a `bg-brand-cyan/10 text-brand-cyan` (o `chip-active`)
- Modal: ghost-input, btn-gold
- Delete modal: mantenere red semantico

### 4.11 cinemas.html

**Modifiche chiave:** Stesse pattern di films.html:
- Stats: `card-elevated` con icona `bg-brand-cyan`
- Table: tonal surfaces
- Modal: ghost-input, btn-gold

### 4.12 proiezioni.html

**Modifiche chiave:** Stesse pattern di films.html:
- Stats: `card-elevated` con icona `bg-brand-indigo-light`
- Table: tonal surfaces
- Badge disponibilita: da `bg-blue-100 text-blue-800` a `chip-active`
- Modal: ghost-input, btn-gold

---

## 5) Modifiche JavaScript (Minime)

### 5.1 js/utils.js

Toast colors: mantenere palette semantica esistente (emerald/red/amber/blue) per non confondere significati.
Opzionale: usare `bg-brand-gold` per success per coerenza visiva. Valutare in fase implementativa.

### 5.2 js/navbar.js

Aggiornare `setActiveNavLink()` per classi CSS gold:
- Admin active: da `text-indigo-600 border-b-2 border-indigo-600` a `text-brand-gold border-b-2 border-brand-gold`
- Admin inactive: da `text-slate-500 hover:text-indigo-600` a `text-brand-on-surface-variant hover:text-brand-gold`
- Mobile active: da `text-indigo-600 bg-indigo-50` a `text-brand-gold bg-brand-surface-container`

### 5.3 js/pages/home.js

Aggiornare `renderFilms()` template literal:
- Card wrapper: da `bg-brand-dark-card rounded-2xl border border-white/10` a `card-elevated`
- Card text: da `text-white` a `text-brand-on-surface`
- Subtitle: da `text-gray-400` a `text-brand-on-surface-variant`
- Genre chip: da `bg-brand-orange` a `bg-brand-gold`
- Prenota button: da `bg-brand-orange hover:bg-brand-orange-dark` a `btn-gold`
- Clock icon text: da `text-gray-400` a `text-brand-on-surface-variant`
- Error message: da `text-white` a `text-brand-on-surface`
- Image overlay: da `from-black/80` a `from-brand-on-surface/30`
- Nessuna modifica alla logica API/rendering

### 5.4 js/pages/films.js

Aggiornare classi CSS inline nei template literal `renderFilms()`:
- Row hover: `hover:bg-brand-surface-container-highest`
- Edit button: `text-brand-gold hover:text-brand-gold-dark`
- Poster placeholder bg: `bg-brand-surface-container`
- Nessuna modifica a logica CRUD, paginazione, filtri, upload

### 5.5 js/pages/registi.js

Aggiornare `renderRegisti()`:
- Row hover: `hover:bg-brand-surface-container-highest`
- Badge nazionalita: `bg-brand-cyan/10 text-brand-cyan` (o mantenere e aggiornare solo in HTML)
- Edit button: `text-brand-gold hover:text-brand-gold-dark`

### 5.6 js/pages/cinemas.js

Aggiornare `renderCinemas()`:
- Row hover: `hover:bg-brand-surface-container-highest`
- Edit button: `text-brand-gold hover:text-brand-gold-dark`

### 5.7 js/pages/proiezioni.js

Aggiornare `renderProiezioni()`:
- Row hover: `hover:bg-brand-surface-container-highest`
- Badge: `chip-active`
- Edit button: `text-brand-gold hover:text-brand-gold-dark`

### 5.8 File NON modificabili (logica pura)

- `js/api.js` — NESSUNA MODIFICA
- `js/template-loader.js` — NESSUNA MODIFICA
- `js/form-handlers.js` — NESSUNA MODIFICA

---

## 6) Checklist Implementazione

### Fase A — Design System Foundation
- [ ] Aggiornare tailwind.config in tutte le 6 pagine HTML con colori Cinema Graphite
- [ ] Verificare Google Fonts link (Inter 300-800, gia presente)
- [ ] Riscrivere css/styles.css con nuove utility classes
- [ ] Mantenere utility esistenti funzionanti

### Fase B — Componenti Condivisi
- [ ] Aggiornare components/navbar-landing.html
- [ ] Aggiornare components/navbar-admin.html
- [ ] Aggiornare components/footer-landing.html
- [ ] Aggiornare components/footer-admin.html

### Fase C — Landing Page (index.html)
- [ ] Body da dark a light theme
- [ ] Hero section: overlay light, testo dark, CTA gold
- [ ] Sezione film: card-elevated, tonal surfaces
- [ ] Filtri: ghost-input style
- [ ] Tailwind config aggiornato

### Fase D — Dashboard (dashboard.html)
- [ ] Sidebar glassmorphism (indigo gradient + blur)
- [ ] Sidebar active/hover con gold accent
- [ ] Header con ambient shadow
- [ ] Stats cards con brand colors
- [ ] Tables con tonal surfaces
- [ ] Modal ghost-input + btn-gold

### Fase E — CRUD Pages
- [ ] Aggiornare films.html
- [ ] Aggiornare registi.html
- [ ] Aggiornare cinemas.html
- [ ] Aggiornare proiezioni.html

### Fase F — JavaScript Adjustments
- [ ] Aggiornare js/navbar.js (classi active gold)
- [ ] Aggiornare js/pages/home.js (card render classi)
- [ ] Aggiornare js/pages/films.js (render classi inline)
- [ ] Aggiornare js/pages/registi.js (render classi inline)
- [ ] Aggiornare js/pages/cinemas.js (render classi inline)
- [ ] Aggiornare js/pages/proiezioni.js (render classi inline)
- [ ] Valutare js/utils.js toast colors

### Fase G — Verifica e Testing
- [ ] Verificare api.js NON modificato
- [ ] Verificare template-loader.js NON modificato
- [ ] Verificare form-handlers.js NON modificato
- [ ] Test manuale index.html
- [ ] Test manuale dashboard.html
- [ ] Test manuale films.html (CRUD + paginazione + upload)
- [ ] Test manuale registi.html
- [ ] Test manuale cinemas.html
- [ ] Test manuale proiezioni.html
- [ ] Verificare responsive design
- [ ] Eseguire dotnet test tests/backend/FilmAPI.Tests.csproj

---

## 7) Criteri di Accettazione

1. Tutte le pagine usano tema LIGHT unificato Cinema Graphite
2. Colori primari: Gold (#D4AF37) per CTA e accent
3. Colori secondari: Indigo Dark (#1A1B2E) per sidebar e depth
4. Sidebar dashboard usa glassmorphism (gradient + blur)
5. Bordi rigidi sostituiti da tonal layering e ambient shadows
6. Input usano ghost borders con gold left-accent al focus
7. Label tabella sono ALL-CAPS con letter-spacing
8. Tutte le funzionalita CRUD funzionano identicamente
9. Paginazione film funziona correttamente
10. Upload copertina funziona correttamente
11. Nessun file JS pura logica e stato modificato
12. Test backend passano tutti

---

## 8) Mappa Colori Quick Reference

| Uso | Prima (Tailwind) | Dopo (Cinema Graphite) |
|-----|------------------|----------------------|
| CTA Primary | bg-indigo-600 | bg-brand-gold / btn-gold |
| CTA Landing | bg-brand-orange | btn-gold |
| Active link admin | text-indigo-600 | text-brand-gold |
| Hover accent | hover:text-indigo-600 | hover:text-brand-gold |
| Background page | bg-slate-50 | bg-brand-surface |
| Card background | bg-white shadow border-gray-100 | card-elevated |
| Table header | bg-gray-50 | bg-brand-surface-container |
| Row hover | hover:bg-slate-50 | hover:bg-brand-surface-container-highest |
| Border section | border-gray-200 | RIMOSSO (tonal layering) |
| Input | border-gray-300 focus:ring-indigo-500 | ghost-input |
| Badge status | bg-emerald-100 text-emerald-800 | chip-active |
| Badge neutral | bg-blue-100 text-blue-800 | bg-brand-surface-container-highest text-brand-on-surface-variant |
| Sidebar bg | bg-slate-900 | sidebar-glass |
| Sidebar active | bg-indigo-50 text-indigo-600 | bg-white/10 text-brand-gold |
| Danger button | bg-red-600 | MANTIENI (semantic) |
| Hero overlay | dark gradient | hero-overlay-light |
| Landing body | bg-brand-dark text-white | bg-brand-surface text-brand-on-surface |
