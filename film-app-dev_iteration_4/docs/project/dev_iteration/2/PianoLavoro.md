# Piano di Lavoro - Iterazione 2: Frontend Web App

## 1) Panoramica

Questa iterazione implementa il frontend dell'applicazione CineBase come Web App didattica con:
- **Backend esistente**: FilmAPI (ASP.NET Core Minimal API con MariaDB)
- **Frontend nuovo**: Applicazione ASP.NET Core Minimal API per servire pagine statiche
- **Tecnologie**: HTML5, CSS3, JavaScript (vanilla), Tailwind CSS (CDN), Fetch API

### 1.1 Riferimenti Design
I mock UI sono disponibili in `docs/project/dev_iteration/2/stitch_cinebase/`:
- `landing_page_cinebase/` - Home page con hero e griglia film
- `gestione_registi/` - CRUD Registi con tabella
- `gestione_film/` - CRUD Film con poster
- `cinema_e_proiezioni/` - Gestione Cinema e Proiezioni
- `dashboard_amministrativa/` - Dashboard amministrativa con statistiche

## 2) Setup Progetto Frontend

### 2.1 Creazione Progetto
- Creare un nuovo progetto ASP.NET Core Empty chiamato `CineBase.Web`
- Configurare middleware per file statici (`UseStaticFiles`, `UseDefaultFiles`)
- Configurare `.NET 9`

### 2.2 Struttura Cartelle (Refactor Consigliato)

```
repo-root/
├── .claude/
├── .git/
├── backend/
│   └── FilmAPI/
│       ├── .env
│       ├── .env.example
│       ├── FilmAPI.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Data/
│       ├── DTO/
│       ├── Endpoints/
│       ├── Migrations/
│       ├── Model/
│       ├── Properties/
│       └── Services/
├── frontend/
│   └── CineBase.Web/
│       ├── .env
│       ├── .env.example
│       ├── CineBase.Web.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Properties/
│       │   └── launchSettings.json
│       └── wwwroot/
│           ├── components/
│           │   ├── navbar-admin.html
│           │   ├── navbar-landing.html
│           │   ├── footer-admin.html
│           │   └── footer-landing.html
│           ├── css/
│           │   └── styles.css
│           ├── js/
│           │   ├── navbar.js
│           │   ├── template-loader.js
│           │   ├── api.js
│           │   ├── utils.js
│           │   ├── form-handlers.js
│           │   └── pages/
│           │       ├── home.js
│           │       ├── films.js
│           │       ├── registi.js
│           │       ├── cinemas.js
│           │       └── proiezioni.js
│           ├── index.html
│           ├── films.html
│           ├── registi.html
│           ├── cinemas.html
│           ├── proiezioni.html
│           └── dashboard.html
├── tests/
│   └── backend/
│       ├── FilmAPI.Tests.csproj
│       ├── Unit/
│       └── Integration/
└── docs/
```

### 2.3 Configurazione Tailwind CSS (CDN)

Tailwind CSS viene caricato via CDN senza build step:

```html
<!-- In ogni pagina HTML -->
<script src="https://cdn.tailwindcss.com?plugins=forms,container-queries"></script>
<script>
  tailwind.config = {
    theme: {
      extend: {
        colors: {
          brand: {
            orange: '#ec5b13',
            'orange-dark': '#d45110',
            dark: '#121212',
            'dark-lighter': '#1E1E1E',
            'dark-card': '#2A2A2A',
            primary: '#1e293b',
            secondary: '#334155',
            accent: '#3b82f6'
          }
        }
      }
    }
  }
</script>
```

### 2.4 Dipendenze Frontend (CDN)

Nessuna installazione npm/bower richiesta. Usare CDN:

### Tailwind CSS v3
```html
<script src="https://cdn.tailwindcss.com?plugins=forms,container-queries"></script>
```

### Google Fonts (Inter)
```html
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700;800&display=swap" rel="stylesheet">
```

### Font Awesome (Icone)
```html
<link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet">
```

### Heroicons / Lucide (Icone SVG)
```html
<script src="https://unpkg.com/lucide@latest"></script>
```

## 3) Design System Tailwind

### 3.1 Configurazione Colori
```javascript
tailwind.config = {
  theme: {
    extend: {
      colors: {
        brand: {
          // Tema scuro (landing)
          orange: '#FF8C00',        // Primary CTA
          'orange-dark': '#E67E00', // Hover stato
          dark: '#121212',         // Background principale
          'dark-lighter': '#1E1E1E', // Card background
          'dark-card': '#2A2A2A',  // Card elevata
          
          // Tema chiaro (admin)
          primary: '#1e293b',      // Slate 800
          secondary: '#334155',    // Slate 700
          accent: '#3b82f6'        // Blue 500
        }
      }
    }
  }
}
```

### 3.2 Classi Tailwind Comuni

#### Background
```html
<!-- Tema scuro (landing page) -->
<body class="bg-brand-dark text-white">
<body class="bg-brand-dark-lighter">
<body class="bg-brand-dark-card">

<!-- Tema chiaro (admin pages) -->
<body class="bg-gray-50 text-gray-900">
<body class="bg-slate-50 text-slate-900">
```

#### Typography
```html
<h1 class="text-2xl font-bold text-slate-900">
<p class="text-sm text-slate-500">
<span class="text-xl font-semibold text-white">
```

#### Cards
```html
<div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
<div class="bg-brand-dark-card rounded-2xl border border-white/10">
```

#### Tables
```html
<table class="min-w-full divide-y divide-slate-200">
<thead class="bg-slate-50">
<tr class="hover:bg-slate-50 transition-colors">
```

#### Buttons
```html
<!-- Primary -->
<button class="bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-lg font-medium shadow-sm transition-all">

<!-- Secondary -->
<button class="bg-white border border-slate-300 text-slate-700 hover:bg-slate-50 px-4 py-2 rounded-lg font-medium">

<!-- Danger -->
<button class="text-red-600 hover:text-red-900 font-medium">
```

#### Badges/Tags
```html
<span class="px-2.5 py-0.5 rounded-full text-xs font-medium bg-emerald-100 text-emerald-800">
<span class="px-2.5 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-800">
<span class="bg-brand-orange text-xs font-bold px-2 py-1 rounded">
```

### 3.3 Responsive Design
```html
<!-- Grid responsivo -->
<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">

<!-- Padding responsivo -->
<div class="px-4 sm:px-6 lg:px-8">

<!-- Text responsivo -->
<h1 class="text-2xl md:text-3xl lg:text-4xl font-bold">
```

## 4) Pagine Frontend

### 4.1 Struttura Base HTML

```html
<!DOCTYPE html>
<html lang="it">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>CineBase - [Nome Pagina]</title>
  
  <!-- Tailwind CSS CDN -->
  <script src="https://cdn.tailwindcss.com?plugins=forms,container-queries"></script>
  
  <!-- Tailwind Config -->
  <script>
    tailwind.config = {
      theme: {
        extend: {
          colors: {
            brand: {
              orange: '#FF8C00',
              'orange-dark': '#E67E00',
              dark: '#121212',
              'dark-lighter': '#1E1E1E',
              'dark-card': '#2A2A2A'
            }
          }
        }
      }
    }
  </script>
  
  <!-- Google Fonts -->
  <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700;800&display=swap" rel="stylesheet">
  
  <!-- Font Awesome -->
  <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet">
  
  <!-- Custom CSS -->
  <link href="/css/styles.css" rel="stylesheet">
</head>
<body class="bg-slate-50 text-slate-900 font-sans">
  <!-- Navbar Container -->
  <div id="navbar-container"></div>
  
  <!-- Main Content -->
  <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
    <!-- Content specifico della pagina -->
  </main>
  
  <!-- Footer Container -->
  <div id="footer-container"></div>
  
  <!-- Toast Container -->
  <div id="toast-container" class="fixed bottom-4 right-4 z-50"></div>
  
  <!-- Custom Scripts -->
  <script src="/js/utils.js"></script>
  <script src="/js/api.js"></script>
  <script src="/js/template-loader.js"></script>
  <script src="/js/form-handlers.js"></script>
  <script src="/js/navbar.js"></script>
  <script src="/js/pages/[page].js"></script>
</body>
</html>
```

### 4.2 Home Page (`index.html`)

**Layout (da mock `landing_page_bootstrap`):**

```
┌─────────────────────────────────────────────────────────────┐
│  NAVBAR (sticky, backdrop-blur)                              │
│  Logo 🎬 CineBase | Home | Film | Registi | Cinema | [Login]│
├─────────────────────────────────────────────────────────────┤
│  HERO SECTION (80vh, bg-image + overlay)                    │
│  "La Tua Rete Cinematografica, Gestita in un Click"        │
│  Background gradient: linear-gradient(rgba(18,18,18,0.6)...) │
├─────────────────────────────────────────────────────────────┤
│  FILM IN PROGRAMMAZIONE                                     │
│  Titolo sezione + linea arancione                           │
│  Filtri: [Città ▼] [Data ▼] [Orario ▼]                     │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐       │
│  │ [poster] │ │ [poster] │ │ [poster] │ │ [poster] │       │
│  │ Titolo   │ │ Titolo   │ │ Titolo   │ │ Titolo   │       │
│  │ Cinema   │ │ Cinema   │ │ Cinema   │ │ Cinema   │       │
│  │ [orari]  │ │ [orari]  │ │ [orari]  │ │ [orari]  │       │
│  │ [Prenota]│ │ [Prenota]│ │ [Prenota]│ │ [Prenota]│       │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘       │
├─────────────────────────────────────────────────────────────┤
│  FOOTER                                                      │
│  Logo | Navigazione | Supporto | Social | Copyright         │
└─────────────────────────────────────────────────────────────┘
```

**Classi Tailwind chiave:**
```html
<!-- Navbar -->
<nav class="sticky top-0 z-50 bg-brand-dark/95 backdrop-blur-md border-b border-white/10">

<!-- Hero -->
<section class="relative h-[80vh] min-h-[600px] flex items-center overflow-hidden">
<div class="hero-overlay absolute inset-0 bg-gradient-to-r from-brand-dark/90 to-brand-dark/60">

<!-- Movie Card -->
<div class="bg-brand-dark-card rounded-2xl overflow-hidden border border-white/10 group transition-all hover:border-brand-orange/50">

<!-- Filter Select -->
<select class="w-full bg-brand-dark border-white/10 rounded-lg text-white focus:ring-brand-orange">
```

### 4.3 Gestione Registi (`registi.html`)

**Layout (da mock `gestione_registi_bootstrap`):**

```
┌─────────────────────────────────────────────────────────────┐
│  HEADER (sticky, white bg)                                   │
│  Logo CineBase | Director Management | [Add Director]       │
├─────────────────────────────────────────────────────────────┤
│  STATS GRID (3 colonne)                                      │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐        │
│  │ Total Directors│ │ Nationalities │ │ Associated Films │ │
│  │     1,284     │ │      42      │ │     3,492    │       │
│  └──────────────┘ └──────────────┘ └──────────────┘        │
├─────────────────────────────────────────────────────────────┤
│  SEARCH & FILTERS                                            │
│  [🔍 Search...] Sort by: [Recently Added ▼]                │
├─────────────────────────────────────────────────────────────┤
│  TABLE                                                       │
│  ┌────┬──────────┬───────────┬────────────┬───────────┐    │
│  │ ID │ Nome     │ Cognome   │ Nazionalità │ Film │ Azioni │ │
│  ├────┼──────────┼───────────┼────────────┼─────────┼───────┤ │
│  │ #1 │ Steven   │ Spielberg │ 🇺🇸 USA    │ 12      │ ✏️ 🗑️│ │
│  └────┴──────────┴───────────┴────────────┴─────────┴───────┘ │
│  Pagination: [<] [1] [2] [3] ... [128] [>]                  │
├─────────────────────────────────────────────────────────────┤
│  FOOTER                                                      │
└─────────────────────────────────────────────────────────────┘
```

**Classi Tailwind chiave:**
```html
<!-- Stats Card -->
<div class="bg-white overflow-hidden shadow rounded-lg border border-gray-100">
  <div class="p-5 flex items-center">
    <div class="flex-shrink-0 bg-indigo-500 rounded-md p-3">

<!-- Table -->
<table class="min-w-full divide-y divide-gray-200">
<thead class="bg-gray-50">
<tr class="hover:bg-gray-50 transition-colors">

<!-- Badge Nazionalità -->
<span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
```

### 4.4 Gestione Film (`films.html`)

**Layout (da mock `gestione_film_bootstrap`):**

```
┌─────────────────────────────────────────────────────────────┐
│  NAVBAR                                                      │
│  Logo | Dashboard | [Movies] | Directors | Analytics        │
├─────────────────────────────────────────────────────────────┤
│  PAGE HEADER                                                 │
│  "Movies Management" + [Export] [Add Movie]                  │
├─────────────────────────────────────────────────────────────┤
│  STATS GRID (4 colonne)                                       │
│  [Total Movies] [New Releases] [Avg Rating] [Genres]        │
├─────────────────────────────────────────────────────────────┤
│  TABLE CONTROLS                                              │
│  [🔍 Search...] [All Genres ▼] [Filters]                    │
├─────────────────────────────────────────────────────────────┤
│  TABLE                                                       │
│  ┌────┬─────────┬──────────┬──────────┬────────┬────────┬───┐ │
│  │ ID │ Poster  │ Titolo    │ Data     │ Regista│ Durata │...│ │
│  ├────┼─────────┼──────────┼──────────┼────────┼────────┼───┤ │
│  │ #1 │ [img]   │ Interstel│ 2024-03  │ Nolan  │ 169min │✏️🗑️│ │
│  └────┴─────────┴──────────┴──────────┴────────┴────────┴───┘ │
├─────────────────────────────────────────────────────────────┤
│  FOOTER                                                      │
└─────────────────────────────────────────────────────────────┘
```

**Thumbnail poster:**
```html
<div class="h-10 w-8 flex-shrink-0 bg-slate-200 rounded overflow-hidden">
  <img class="h-full w-full object-cover" src="poster.jpg" alt="Movie">
</div>
```

### 4.5 Gestione Cinema & Proiezioni (`cinemas_proiezioni.html`)

**Layout (da mock `cinema_e_proiezioni_bootstrap`):**

```
┌─────────────────────────────────────────────────────────────┐
│  HEADER                                                      │
│  CineBase Management Console | [New Entry]                   │
├─────────────────────────────────────────────────────────────┤
│  CINEMAS SECTION                                             │
│  🏢 Cinemas (Total: 4)                                       │
│  ┌────┬─────────────┬──────────────────┬─────────┬────────┐  │
│  │ ID │ Name        │ Address          │ City    │ Actions│  │
│  ├────┼─────────────┼──────────────────┼─────────┼────────┤  │
│  │ C-1│ Grand Rex   │ 1 Blvd Poissonière│ Paris  │ ✏️ 🗑️ │  │
│  └────┴─────────────┴──────────────────┴─────────┴────────┘  │
├─────────────────────────────────────────────────────────────┤
│  SCREENINGS SECTION                                          │
│  📹 Screenings [All Cinemas ▼]                               │
│  ┌──────┬───────────┬───────────────┬──────────┬───────┬─────┐ │
│  │ ID   │ Cinema     │ Film          │ Date     │ Ora   │... │ │
│  ├──────┼───────────┼───────────────┼──────────┼───────┼─────┤ │
│  │ S-001│ Grand Rex │ Dune Pt.2     │ Oct 24   │ 20:30 │... │ │
│  └──────┴───────────┴───────────────┴──────────┴───────┴─────┘ │
├─────────────────────────────────────────────────────────────┤
│  FOOTER STATS                                                │
│  [Tickets: 1,429] [Revenue: $12,840] [Members: 42]          │
└─────────────────────────────────────────────────────────────┘
```

### 4.6 Dashboard (`dashboard.html`)

**Layout (da mock `dashboard_bootstrap`):**

```
┌────────────┬─────────────────────────────────────────────────┐
│  SIDEBAR   │  MAIN CONTENT                                  │
│  ┌──────┐  │  ┌─────────────────────────────────────────────┐│
│  │🎬    │  │  │ Overview                    [Add Screening] ││
│  │CineM │  │  ├─────────────────────────────────────────────┤│
│  │anage │  │  │ STATS GRID (4 cards)                         ││
│  └──────┘  │  │ [Movies: 1,248] [Directors: 342]            ││
│  Dashboard │  │ [Cinemas: 24] [Screenings: 112]             ││
│  Movies    │  ├─────────────────────────────────────────────┤│
│  Directors │  │ UPCOMING SCREENINGS TABLE                    ││
│  Cinemas   │  │ Movie | Cinema | Time | Status              ││
│  Screenings│  │ ...                                         ││
│  Settings  │  ├─────────────────────────────────────────────┤│
│            │  │ TRENDING DIRECTORS | CINEMA INSIGHTS        ││
│  [Profile] │  │ [Director list]    | [Stats bars]         ││
└────────────┴─────────────────────────────────────────────────┘
```

**Sidebar Tailwind:**
```html
<aside class="w-64 bg-slate-900 text-slate-300 flex-shrink-0 flex flex-col sticky top-0 h-screen">
  <nav class="flex-1 px-4 py-4 space-y-1">
    <a class="flex items-center gap-3 px-4 py-3 rounded-xl bg-indigo-50 text-indigo-600 font-semibold">
    <a class="flex items-center gap-3 px-4 py-3 rounded-xl hover:bg-slate-800 hover:text-white">
```

## 5) Moduli JavaScript

### 5.1 `api.js` - Fetch API Module

```javascript
// Configurazione base
const API_BASE_URL = 'http://localhost:5000';

// Helper function per fetch con error handling
async function apiFetch(endpoint, options = {}) {
  const defaultOptions = {
    headers: {
      'Content-Type': 'application/json'
    }
  };
  
  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    ...defaultOptions,
    ...options
  });
  
  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'Errore di rete' }));
    throw { status: response.status, ...error };
  }
  
  if (response.status === 204) return null;
  return response.json();
}

// API Object
const API = {
  // Registi
  getRegisti: () => apiFetch('/registi'),
  getRegista: (id) => apiFetch(`/registi/${id}`),
  createRegista: (data) => apiFetch('/registi', { 
    method: 'POST', 
    body: JSON.stringify(data) 
  }),
  updateRegista: (id, data) => apiFetch(`/registi/${id}`, { 
    method: 'PUT', 
    body: JSON.stringify(data) 
  }),
  deleteRegista: (id) => apiFetch(`/registi/${id}`, { method: 'DELETE' }),
  getFilmsByRegista: (id) => apiFetch(`/registi/${id}/films`),
  
  // Film
  getFilms: () => apiFetch('/films'),
  getFilm: (id) => apiFetch(`/films/${id}`),
  createFilm: (data) => apiFetch('/films', { 
    method: 'POST', 
    body: JSON.stringify(data) 
  }),
  updateFilm: (id, data) => apiFetch(`/films/${id}`, { 
    method: 'PUT', 
    body: JSON.stringify(data) 
  }),
  deleteFilm: (id) => apiFetch(`/films/${id}`, { method: 'DELETE' }),
  
  // Cinema
  getCinemas: () => apiFetch('/cinemas'),
  getCinema: (id) => apiFetch(`/cinemas/${id}`),
  createCinema: (data) => apiFetch('/cinemas', { 
    method: 'POST', 
    body: JSON.stringify(data) 
  }),
  updateCinema: (id, data) => apiFetch(`/cinemas/${id}`, { 
    method: 'PUT', 
    body: JSON.stringify(data) 
  }),
  deleteCinema: (id) => apiFetch(`/cinemas/${id}`, { method: 'DELETE' }),
  
  // Proiezioni
  getProiezioni: () => apiFetch('/proiezioni'),
  getProiezione: (id) => apiFetch(`/proiezioni/${id}`),
  createProiezione: (data) => apiFetch('/proiezioni', { 
    method: 'POST', 
    body: JSON.stringify(data) 
  }),
  updateProiezione: (id, data) => apiFetch(`/proiezioni/${id}`, { 
    method: 'PUT', 
    body: JSON.stringify(data) 
  }),
  deleteProiezione: (id) => apiFetch(`/proiezioni/${id}`, { method: 'DELETE' })
};
```

### 5.2 `template-loader.js` - Component Loader

```javascript
// Cache per i template caricati
const templateCache = {};

async function loadComponent(elementId, componentPath) {
  if (templateCache[componentPath]) {
    document.getElementById(elementId).innerHTML = templateCache[componentPath];
    return;
  }
  
  try {
    const response = await fetch(componentPath);
    if (!response.ok) throw new Error(`Errore caricamento ${componentPath}`);
    
    const html = await response.text();
    templateCache[componentPath] = html;
    document.getElementById(elementId).innerHTML = html;
    
    // Esegui script inline se presenti
    const scripts = document.getElementById(elementId).querySelectorAll('script');
    scripts.forEach(script => {
      const newScript = document.createElement('script');
      newScript.textContent = script.textContent;
      script.parentNode.replaceChild(newScript, script);
    });
  } catch (error) {
    console.error('Errore caricamento componente:', error);
  }
}

// Carica navbar e footer all'avvio
document.addEventListener('DOMContentLoaded', () => {
  loadComponent('navbar-container', '/components/navbar.html');
  loadComponent('footer-container', '/components/footer.html');
});
```

### 5.3 `utils.js` - Utility Functions

```javascript
// Formattazione data ISO -> DD/MM/YYYY
function formatDate(isoDate) {
  if (!isoDate) return '-';
  const date = new Date(isoDate);
  return date.toLocaleDateString('it-IT');
}

// Formattazione data per input date (YYYY-MM-DD)
function formatDateForInput(isoDate) {
  if (!isoDate) return '';
  return isoDate.split('T')[0];
}

// Formattazione ora (HH:MM)
function formatTime(timeString) {
  if (!timeString) return '-';
  return timeString.substring(0, 5);
}

// Troncamento testo
function truncateText(text, maxLength = 50) {
  if (!text) return '';
  return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
}

// Gestione errori API
function handleApiError(error) {
  console.error('API Error:', error);
  
  let message = 'Si è verificato un errore';
  
  switch (error.status) {
    case 400:
      message = error.errors ? Object.values(error.errors).join(', ') : 'Dati non validi';
      break;
    case 404:
      message = 'Elemento non trovato';
      break;
    case 409:
      message = 'Elemento già esistente (conflitto)';
      break;
    case 500:
      message = 'Errore del server';
      break;
  }
  
  showToast(message, 'danger');
  return message;
}

// Toast notification (Tailwind version)
function showToast(message, type = 'success') {
  const toastContainer = document.getElementById('toast-container');
  if (!toastContainer) return;
  
  const colors = {
    success: 'bg-emerald-500',
    danger: 'bg-red-500',
    warning: 'bg-amber-500',
    info: 'bg-blue-500'
  };
  
  const toastId = 'toast-' + Date.now();
  const toastHtml = `
    <div id="${toastId}" class="${colors[type]} text-white px-6 py-3 rounded-lg shadow-lg flex items-center gap-3 animate-fade-in">
      <span>${message}</span>
      <button onclick="this.parentElement.remove()" class="hover:bg-white/20 rounded p-1">
        <i class="fa-solid fa-xmark"></i>
      </button>
    </div>
  `;
  
  toastContainer.insertAdjacentHTML('beforeend', toastHtml);
  
  // Auto-remove after 3 seconds
  setTimeout(() => {
    const toast = document.getElementById(toastId);
    if (toast) toast.remove();
  }, 3000);
}

// Conferma eliminazione
function confirmDelete(itemName, callback) {
  const confirmed = confirm(`Sei sicuro di voler eliminare "${itemName}"?`);
  if (confirmed) callback();
}
```

### 5.4 `form-handlers.js` - Form Management

```javascript
// Popola select da dati API
function populateSelect(selectId, data, valueField = 'id', labelFields = ['nome'], placeholder = 'Seleziona...') {
  const select = document.getElementById(selectId);
  if (!select) return;
  
  // Mantieni opzione placeholder
  select.innerHTML = `<option value="">${placeholder}</option>`;
  
  data.forEach(item => {
    const label = labelFields.map(field => item[field]).join(' ');
    select.innerHTML += `<option value="${item[valueField]}">${label}</option>`;
  });
}

// Prepara form per creazione
function setupCreateForm(modalId, formId, fields) {
  const form = document.getElementById(formId);
  form.reset();
  form.dataset.editId = '';
}

// Prepara form per modifica
function setupEditForm(modalId, formId, data, fields) {
  const form = document.getElementById(formId);
  form.dataset.editId = data.id;
  
  fields.forEach(field => {
    const input = form.querySelector(`[name="${field}"]`);
    if (input) {
      input.value = data[field] ?? '';
    }
  });
}

// Serializza form in oggetto
function serializeForm(formId) {
  const form = document.getElementById(formId);
  const formData = new FormData(form);
  const data = {};
  
  for (let [key, value] of formData.entries()) {
    data[key] = value;
  }
  
  return data;
}

// Setup submit handler
function setupFormSubmit(formId, apiCreate, apiUpdate, onSuccess) {
  const form = document.getElementById(formId);
  
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    if (!form.checkValidity()) {
      form.classList.add('was-validated');
      return;
    }
    
    const data = serializeForm(formId);
    const editId = form.dataset.editId;
    
    try {
      if (editId) {
        await apiUpdate(editId, data);
        showToast('Elemento aggiornato con successo');
      } else {
        await apiCreate(data);
        showToast('Elemento creato con successo');
      }
      
      // Chiudi modal (Tailwind compatible)
      const modalElement = document.getElementById(formId.replace('-form', '-modal'));
      modalElement.classList.add('hidden');
      
      onSuccess();
    } catch (error) {
      handleApiError(error);
    }
  });
}
```

### 5.5 `navbar.js` - Navigation Logic

```javascript
// Gestione stato attivo navbar
function setActiveNavLink() {
  const currentPath = window.location.pathname;
  const navLinks = document.querySelectorAll('nav a');
  
  navLinks.forEach(link => {
    const href = link.getAttribute('href');
    if (href === currentPath || (currentPath === '/' && href === '/index.html')) {
      link.classList.add('bg-slate-800', 'text-white');
      link.classList.remove('text-slate-500', 'hover:bg-slate-800', 'hover:text-white');
    } else {
      link.classList.remove('bg-slate-800', 'text-white');
      link.classList.add('text-slate-500', 'hover:bg-slate-800', 'hover:text-white');
    }
  });
}

// Mobile menu toggle
function setupMobileMenu() {
  const menuToggle = document.getElementById('mobile-menu-toggle');
  const mobileMenu = document.getElementById('mobile-menu');
  
  if (menuToggle && mobileMenu) {
    menuToggle.addEventListener('click', () => {
      mobileMenu.classList.toggle('hidden');
    });
  }
}

// Mock auth state
function updateAuthUI() {
  const user = JSON.parse(sessionStorage.getItem('user') || 'null');
  const loginBtn = document.getElementById('login-btn');
  const userDropdown = document.getElementById('user-dropdown');
  
  if (user) {
    if (loginBtn) loginBtn.classList.add('hidden');
    if (userDropdown) {
      userDropdown.classList.remove('hidden');
      document.getElementById('user-name').textContent = user.name;
    }
  } else {
    if (loginBtn) loginBtn.classList.remove('hidden');
    if (userDropdown) userDropdown.classList.add('hidden');
  }
}

function mockLogin() {
  sessionStorage.setItem('user', JSON.stringify({ 
    id: 1, 
    name: 'Admin', 
    role: 'administrator' 
  }));
  updateAuthUI();
}

function mockLogout() {
  sessionStorage.removeItem('user');
  window.location.href = '/index.html';
}
```

## 6) Componenti HTML Riutilizzabili

### 6.1 `navbar.html` - Navbar Admin

```html
<!-- Tailwind Navbar per Admin Pages -->
<nav class="bg-white border-b border-slate-200 sticky top-0 z-40">
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
    <div class="flex justify-between h-16">
      <!-- Logo -->
      <div class="flex items-center gap-8">
        <a href="/index.html" class="flex items-center gap-2">
          <div class="w-10 h-10 bg-indigo-600 rounded-lg flex items-center justify-center text-white">
            <i class="fa-solid fa-film"></i>
          </div>
          <span class="text-xl font-bold text-slate-900">CineBase</span>
        </a>
        
        <!-- Desktop Navigation -->
        <div class="hidden md:flex items-center space-x-4">
          <a href="/dashboard.html" class="nav-link px-3 py-2 text-sm font-medium text-slate-500 hover:text-indigo-600 transition-colors">
            Dashboard
          </a>
          <a href="/films.html" class="nav-link px-3 py-2 text-sm font-medium text-indigo-600 border-b-2 border-indigo-600">
            Film
          </a>
          <a href="/registi.html" class="nav-link px-3 py-2 text-sm font-medium text-slate-500 hover:text-indigo-600 transition-colors">
            Registi
          </a>
          <a href="/cinemas.html" class="nav-link px-3 py-2 text-sm font-medium text-slate-500 hover:text-indigo-600 transition-colors">
            Cinema
          </a>
          <a href="/proiezioni.html" class="nav-link px-3 py-2 text-sm font-medium text-slate-500 hover:text-indigo-600 transition-colors">
            Proiezioni
          </a>
        </div>
      </div>
      
      <!-- Right Side -->
      <div class="flex items-center gap-4">
        <!-- Notifications -->
        <button class="p-2 text-slate-400 hover:text-slate-600">
          <i class="fa-regular fa-bell text-lg"></i>
        </button>
        
        <!-- User Menu -->
        <div class="relative" id="user-dropdown">
          <button class="flex items-center gap-2 text-sm font-medium text-slate-700 hover:text-slate-900">
            <div class="w-8 h-8 bg-indigo-100 rounded-full flex items-center justify-center text-indigo-700 font-semibold">
              AD
            </div>
            <span class="hidden sm:inline" id="user-name">Admin</span>
          </button>
        </div>
        
        <!-- Login Button (shown when not logged in) -->
        <button id="login-btn" class="hidden bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-lg text-sm font-medium transition-colors">
          Login
        </button>
      </div>
      
      <!-- Mobile Menu Toggle -->
      <div class="md:hidden flex items-center">
        <button id="mobile-menu-toggle" class="p-2 text-slate-400 hover:text-slate-600">
          <i class="fa-solid fa-bars text-xl"></i>
        </button>
      </div>
    </div>
  </div>
  
  <!-- Mobile Menu -->
  <div id="mobile-menu" class="hidden md:hidden border-t border-slate-200">
    <div class="px-2 pt-2 pb-3 space-y-1">
      <a href="/dashboard.html" class="block px-3 py-2 text-base font-medium text-slate-700 hover:bg-slate-50">Dashboard</a>
      <a href="/films.html" class="block px-3 py-2 text-base font-medium text-indigo-600 bg-indigo-50">Film</a>
      <a href="/registi.html" class="block px-3 py-2 text-base font-medium text-slate-700 hover:bg-slate-50">Registi</a>
      <a href="/cinemas.html" class="block px-3 py-2 text-base font-medium text-slate-700 hover:bg-slate-50">Cinema</a>
      <a href="/proiezioni.html" class="block px-3 py-2 text-base font-medium text-slate-700 hover:bg-slate-50">Proiezioni</a>
    </div>
  </div>
</nav>
```

### 6.2 `navbar.html` - Navbar Landing (Dark Theme)

```html
<!-- Tailwind Navbar per Landing Page (Dark Theme) -->
<nav class="sticky top-0 z-50 bg-brand-dark/95 backdrop-blur-md border-b border-white/10">
  <div class="container mx-auto px-4 h-20 flex items-center justify-between">
    <!-- Logo -->
    <a href="/index.html" class="text-2xl font-bold text-brand-orange tracking-tight flex items-center gap-2">
      <span class="text-3xl">🎬</span>
      CineBase
    </a>
    
    <!-- Desktop Links -->
    <div class="hidden md:flex items-center gap-8">
      <a href="#programmazione" class="hover:text-brand-orange transition-colors font-medium text-white">
        Programmazione
      </a>
      <a href="/films.html" class="hover:text-brand-orange transition-colors font-medium text-white">
        Film
      </a>
      <a href="/cinemas.html" class="hover:text-brand-orange transition-colors font-medium text-white">
        Sale
      </a>
      <a href="/dashboard.html" class="bg-brand-orange hover:bg-brand-orange-dark px-6 py-2.5 rounded-lg font-semibold text-white transition-all shadow-lg shadow-brand-orange/20">
        Area Admin
      </a>
    </div>
    
    <!-- Mobile Menu Toggle -->
    <div class="md:hidden">
      <button id="mobile-menu-toggle" class="p-2 text-white">
        <i class="fa-solid fa-bars text-2xl"></i>
      </button>
    </div>
  </div>
  
  <!-- Mobile Menu -->
  <div id="mobile-menu" class="hidden md:hidden bg-brand-dark-lighter border-t border-white/10">
    <div class="px-4 py-4 space-y-3">
      <a href="#programmazione" class="block py-2 text-white hover:text-brand-orange">Programmazione</a>
      <a href="/films.html" class="block py-2 text-white hover:text-brand-orange">Film</a>
      <a href="/cinemas.html" class="block py-2 text-white hover:text-brand-orange">Sale</a>
      <a href="/dashboard.html" class="block py-2 bg-brand-orange text-white text-center rounded-lg">Area Admin</a>
    </div>
  </div>
</nav>
```

### 6.3 `footer.html` - Footer Admin

```html
<!-- Footer Admin Pages -->
<footer class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 border-t border-slate-200 mt-8">
  <div class="flex flex-col md:flex-row justify-between items-center gap-4">
    <div class="text-sm text-slate-400">
      © 2024 CineBase Administration. All rights reserved.
    </div>
    <div class="flex gap-6 text-sm text-slate-400">
      <a href="#" class="hover:text-indigo-600 transition-colors">Privacy Policy</a>
      <a href="#" class="hover:text-indigo-600 transition-colors">Documentation</a>
      <a href="#" class="hover:text-indigo-600 transition-colors">Support</a>
    </div>
  </div>
</footer>
```

### 6.4 `footer.html` - Footer Landing (Dark Theme)

```html
<!-- Footer Landing Page (Dark Theme) -->
<footer class="bg-brand-dark-lighter border-t border-white/10 pt-16 pb-8">
  <div class="container mx-auto px-4">
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-12 mb-12">
      <!-- Brand Info -->
      <div class="space-y-4">
        <a href="/index.html" class="text-2xl font-bold text-brand-orange tracking-tight flex items-center gap-2">
          🎬 CineBase
        </a>
        <p class="text-gray-400 leading-relaxed">
          La piattaforma leader per la gestione centralizzata delle sale cinematografiche.
        </p>
      </div>
      
      <!-- Quick Links -->
      <div>
        <h4 class="text-lg font-bold mb-6 text-white">Navigazione</h4>
        <ul class="space-y-3 text-gray-400">
          <li><a href="/index.html" class="hover:text-brand-orange transition-colors">Home</a></li>
          <li><a href="/films.html" class="hover:text-brand-orange transition-colors">Film</a></li>
          <li><a href="/cinemas.html" class="hover:text-brand-orange transition-colors">Cinema</a></li>
          <li><a href="/dashboard.html" class="hover:text-brand-orange transition-colors">Admin</a></li>
        </ul>
      </div>
      
      <!-- Support -->
      <div>
        <h4 class="text-lg font-bold mb-6 text-white">Supporto</h4>
        <ul class="space-y-3 text-gray-400">
          <li><a href="#" class="hover:text-brand-orange transition-colors">Centro Assistenza</a></li>
          <li><a href="#" class="hover:text-brand-orange transition-colors">Termini e Condizioni</a></li>
          <li><a href="#" class="hover:text-brand-orange transition-colors">Privacy Policy</a></li>
        </ul>
      </div>
      
      <!-- Social -->
      <div>
        <h4 class="text-lg font-bold mb-6 text-white">Seguici</h4>
        <div class="flex gap-4 mb-6">
          <a href="#" class="w-10 h-10 rounded-full bg-white/5 flex items-center justify-center hover:bg-brand-orange transition-colors">
            <i class="fa-brands fa-facebook"></i>
          </a>
          <a href="#" class="w-10 h-10 rounded-full bg-white/5 flex items-center justify-center hover:bg-brand-orange transition-colors">
            <i class="fa-brands fa-instagram"></i>
          </a>
          <a href="#" class="w-10 h-10 rounded-full bg-white/5 flex items-center justify-center hover:bg-brand-orange transition-colors">
            <i class="fa-brands fa-twitter"></i>
          </a>
        </div>
        <p class="text-sm text-gray-400">API Docs | Swagger</p>
      </div>
    </div>
    
    <!-- Footer Bottom -->
    <div class="border-t border-white/10 pt-8 flex flex-col md:flex-row justify-between items-center gap-4 text-sm text-gray-500">
      <p>Sviluppato con ASP.NET Core + Tailwind CSS per scopo didattico.</p>
      <p>© 2024 CineBase. Tutti i diritti riservati.</p>
    </div>
  </div>
</footer>
```

## 7) CSS Custom (`styles.css`)

```css
/*
 * CineBase - Custom Styles
 * Tailwind CSS Extensions
 */

/* === Custom Fonts === */
body {
  font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
}

/* === Custom Scrollbar === */
::-webkit-scrollbar {
  width: 6px;
  height: 6px;
}

::-webkit-scrollbar-track {
  background: #f1f1f1;
}

::-webkit-scrollbar-thumb {
  background: #888;
  border-radius: 10px;
}

::-webkit-scrollbar-thumb:hover {
  background: #555;
}

/* === Animations === */
.animate-fade-in {
  animation: fadeIn 0.3s ease-in;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

/* === Hero Overlay === */
.hero-overlay {
  background: linear-gradient(
    to right,
    rgba(18, 18, 18, 0.95),
    rgba(18, 18, 18, 0.7)
  );
}

/* === Table Striped === */
.table-striped tr:nth-child(even) {
  background-color: #f8fafc;
}

/* === Form Validation === */
.form-input-error {
  border-color: #ef4444;
}

.form-input-error:focus {
  ring-color: rgba(239, 68, 68, 0.2);
}

/* === Modal Backdrop === */
.modal-backdrop {
  background-color: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(4px);
}

/* === Card Hover Effect === */
.card-hover {
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.card-hover:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 24px rgba(0, 0, 0, 0.15);
}

/* === Responsive Images === */
img {
  max-width: 100%;
  height: auto;
}

/* === Print Styles === */
@media print {
  .no-print {
    display: none !important;
  }
}
```

## 8) Modal Components (Tailwind)

### 8.1 Modal CRUD Template

```html
<!-- Modal Create/Edit (Tailwind) -->
<div id="entity-modal" class="fixed inset-0 z-50 hidden" aria-labelledby="modal-title" role="dialog" aria-modal="true">
  <!-- Backdrop -->
  <div class="fixed inset-0 bg-gray-500/75 transition-opacity modal-backdrop"></div>
  
  <!-- Modal Content -->
  <div class="fixed inset-0 z-10 overflow-y-auto">
    <div class="flex min-h-full items-end justify-center p-4 text-center sm:items-center sm:p-0">
      <div class="relative transform overflow-hidden rounded-xl bg-white text-left shadow-xl transition-all sm:my-8 sm:w-full sm:max-w-lg">
        
        <!-- Header -->
        <div class="bg-white px-6 pt-6 pb-4">
          <h3 class="text-lg font-bold text-slate-900" id="modal-title">
            Aggiungi Elemento
          </h3>
          <button type="button" class="absolute top-4 right-4 text-slate-400 hover:text-slate-600" onclick="closeModal()">
            <i class="fa-solid fa-xmark text-xl"></i>
          </button>
        </div>
        
        <!-- Body -->
        <div class="bg-white px-6 pb-4">
          <form id="entity-form" class="space-y-4">
            <!-- Campi dinamici qui -->
          </form>
        </div>
        
        <!-- Footer -->
        <div class="bg-slate-50 px-6 py-4 flex justify-end gap-3">
          <button type="button" class="px-4 py-2 bg-white border border-slate-300 rounded-lg text-sm font-medium text-slate-700 hover:bg-slate-50" onclick="closeModal()">
            Annulla
          </button>
          <button type="submit" form="entity-form" class="px-4 py-2 bg-indigo-600 text-white rounded-lg text-sm font-medium hover:bg-indigo-700">
            Salva
          </button>
        </div>
        
      </div>
    </div>
  </div>
</div>

<script>
function openModal(title = 'Aggiungi Elemento') {
  document.getElementById('modal-title').textContent = title;
  document.getElementById('entity-modal').classList.remove('hidden');
}

function closeModal() {
  document.getElementById('entity-modal').classList.add('hidden');
}
</script>
```

### 8.2 Modal Conferma Eliminazione

```html
<!-- Modal Conferma Delete -->
<div id="delete-modal" class="fixed inset-0 z-50 hidden">
  <div class="fixed inset-0 bg-gray-500/75 modal-backdrop"></div>
  
  <div class="fixed inset-0 z-10 overflow-y-auto">
    <div class="flex min-h-full items-end justify-center p-4 text-center sm:items-center sm:p-0">
      <div class="relative transform overflow-hidden rounded-xl bg-white text-left shadow-xl transition-all sm:my-8 sm:w-full sm:max-w-md">
        
        <div class="bg-white px-6 pt-6 pb-4">
          <div class="flex items-center gap-4">
            <div class="flex-shrink-0 flex items-center justify-center h-12 w-12 rounded-full bg-red-100">
              <i class="fa-solid fa-triangle-exclamation text-red-600 text-xl"></i>
            </div>
            <div>
              <h3 class="text-lg font-bold text-slate-900">Conferma Eliminazione</h3>
              <p class="text-sm text-slate-500 mt-1" id="delete-message">
                Sei sicuro di voler eliminare questo elemento?
              </p>
            </div>
          </div>
        </div>
        
        <div class="bg-slate-50 px-6 py-4 flex justify-end gap-3">
          <button type="button" class="px-4 py-2 bg-white border border-slate-300 rounded-lg text-sm font-medium text-slate-700 hover:bg-slate-50" onclick="closeDeleteModal()">
            Annulla
          </button>
          <button type="button" class="px-4 py-2 bg-red-600 text-white rounded-lg text-sm font-medium hover:bg-red-700" id="confirm-delete-btn">
            Elimina
          </button>
        </div>
        
      </div>
    </div>
  </div>
</div>

<script>
function openDeleteModal(itemName, callback) {
  document.getElementById('delete-message').textContent = `Sei sicuro di voler eliminare "${itemName}"?`;
  document.getElementById('delete-modal').classList.remove('hidden');
  
  document.getElementById('confirm-delete-btn').onclick = () => {
    callback();
    closeDeleteModal();
  };
}

function closeDeleteModal() {
  document.getElementById('delete-modal').classList.add('hidden');
}
</script>
```

## 9) Configurazione CORS Backend

Aggiornare il backend FilmAPI (`Program.cs`) per permettere richieste dal frontend:

```csharp
// Aggiungere dopo builder.Services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowCineBaseFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5001", "http://localhost:5173", "http://127.0.0.1:5001")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Aggiungere prima di app.UseSwagger()
app.UseCors("AllowCineBaseFrontend");
```

## 10) Gestione Errori API

### 10.1 Codici HTTP e Azioni Frontend

| Codice | Significato | Azione Frontend |
|--------|-------------|-----------------|
| 200 OK | Operazione riuscita | Aggiornare UI, mostrare toast success |
| 201 Created | Risorsa creata | Chiudere modal, refresh lista, toast |
| 204 No Content | Delete riuscito | Rimuovere riga tabella, toast |
| 400 Bad Request | Validazione fallita | Mostrare errori nel form, toast danger |
| 404 Not Found | Risorsa non trovata | Redirect o messaggio errore |
| 409 Conflict | Vincolo violato | Toast "Elemento già esistente" |

### 10.2 Toast Notification Styles

```javascript
// Tailwind toast colors
const colors = {
  success: 'bg-emerald-500',
  danger: 'bg-red-500',
  warning: 'bg-amber-500',
  info: 'bg-blue-500'
};
```

## 11) Mock Autenticazione

```javascript
// auth.js
const MOCK_USER = {
  id: 1,
  username: 'admin',
  name: 'Admin User',
  role: 'administrator'
};

function isLoggedIn() {
  return sessionStorage.getItem('user') !== null;
}

function getCurrentUser() {
  return JSON.parse(sessionStorage.getItem('user') || 'null');
}

function login(username, password) {
  // Mock - accetta qualsiasi credenziale
  sessionStorage.setItem('user', JSON.stringify(MOCK_USER));
  return MOCK_USER;
}

function logout() {
  sessionStorage.removeItem('user');
  window.location.href = '/index.html';
}
```

## 12) `Program.cs` Frontend

```csharp
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Servire file di default (index.html)
app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = new List<string> { "index.html" }
});

// Servire file statici dalla cartella wwwroot
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    DefaultContentType = "text/html"
});

// Fallback per SPA routing
app.MapFallbackToFile("/index.html");

app.Run();
```

Nota: evitare porte hardcoded in `Program.cs`; usare `ASPNETCORE_URLS` in `.env`/launch settings.

## 13) Checklist Implementazione

### Fase 1: Setup Progetto
- [ ] Creare cartella `CineBase.Web`
- [ ] Eseguire `dotnet new web -n CineBase.Web`
- [ ] Configurare `Program.cs` per static files
- [ ] Creare struttura cartelle `wwwroot/`
- [ ] Creare `css/styles.css` con custom styles
- [ ] Creare `components/navbar.html`
- [ ] Creare `components/footer.html`

### Fase 2: Moduli JavaScript Base
- [ ] Creare `js/utils.js` con utility functions
- [ ] Creare `js/api.js` con Fetch API module
- [ ] Creare `js/template-loader.js` per componenti
- [ ] Creare `js/form-handlers.js` per form management
- [ ] Creare `js/navbar.js` per navigazione

### Fase 3: Pagine Frontend
- [ ] Implementare `index.html` (Home/Landing)
- [ ] Implementare `js/pages/home.js`
- [ ] Implementare `dashboard.html` (Dashboard Admin)
- [ ] Implementare `js/pages/dashboard.js`
- [ ] Implementare `registi.html` + `js/pages/registi.js`
- [ ] Implementare `films.html` + `js/pages/films.js`
- [ ] Implementare `cinemas.html` + `js/pages/cinemas.js`
- [ ] Implementare `proiezioni.html` + `js/pages/proiezioni.js`

### Fase 4: Integrazione Backend
- [ ] Aggiornare CORS in FilmAPI
- [ ] Testare tutti gli endpoint da frontend
- [ ] Verificare gestione errori
- [ ] Testare modale create/edit/delete

### Fase 4.1: Allineamento DTO (Critico)
- [ ] Film frontend: usare campi backend `titolo`, `dataProduzione`, `registaId`, `durata`, `copertinaPath`, `filmatoPath`
- [ ] Cinema frontend: inviare solo `nome`, `indirizzo`, `citta` (escludere `telefono` dal payload)
- [ ] Proiezione frontend: inviare `cinemaId`, `filmId`, `data`, `ora` (normalizzare `ora` come DateTime ISO)
- [ ] Gestire eventuali risposte collection in formati diversi (`array`, `items`, `$values`) prima del render

### Fase 4.2: Stabilità JavaScript (Critico)
- [ ] Evitare assegnazioni con optional chaining a sinistra (esempio invalido: `el?.textContent = ...`)
- [ ] Verificare da DevTools che ogni pagina emetta le chiamate API previste (`GET /registi`, `GET /films`, ...)
- [ ] In caso di errore fetch, mostrare toast con messaggio esplicito (backend non raggiungibile / payload non valido)

### Fase 5: Testing e Finalizzazione
- [ ] Testare responsive design (mobile, tablet, desktop)
- [ ] Verificare accessibilità
- [ ] Ottimizzare performance (lazy loading immagini)
- [ ] Documentare codice inline

## 14) Note Tecniche

### 14.1 Esecuzione Progetti
```bash
# Da root repo

# Terminale 1: Backend
dotnet run --project backend/FilmAPI/FilmAPI.csproj

# Terminale 2: Frontend
dotnet run --project frontend/CineBase.Web/CineBase.Web.csproj
```

Le porte sono configurate tramite env (`ASPNETCORE_URLS`) + launch settings:
- Backend: `http://localhost:5000`
- Frontend: `http://localhost:5001`

### 14.2 Struttura File Statici
```
wwwroot/
├── index.html          # Accessibile a http://localhost:5001/
├── films.html          # Accessibile a http://localhost:5001/films.html
├── css/styles.css      # Accessibile a http://localhost:5001/css/styles.css
└── js/api.js           # Accessibile a http://localhost:5001/js/api.js
```

### 14.3 Dipendenze CDN
Tutte le dipendenze sono caricate via CDN, nessun build step richiesto:
- Tailwind CSS: `https://cdn.tailwindcss.com`
- Font Awesome: `https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0`
- Lucide Icons: `https://unpkg.com/lucide@latest`
- Google Fonts Inter: `https://fonts.googleapis.com/css2?family=Inter`

### 14.4 Browser Support
- Chrome/Edge (ultimi 2 versioni)
- Firefox (ultimi 2 versioni)
- Safari (ultimi 2 versioni)

### 14.5 Best Practice
- Mantenere separazione logica (JS) e presentazione (HTML)
- Usare `async/await` per Fetch API
- Gestire sempre `.catch()` per errori fetch
- Usare classi Tailwind per styling (evitare CSS custom quando possibile)
- Cache dei componenti HTML nel `template-loader.js`
- Lazy loading per immagini poster film

### 14.6 Note per sviluppo assistito da AI
- Specificare esplicitamente all'AI la struttura target del repo (`backend/FilmAPI`, `frontend/CineBase.Web`, `tests/backend`, `docs`).
- Chiedere all'AI di verificare allineamento tra DTO backend e payload frontend prima di implementare i form CRUD.
- Richiedere una verifica esplicita DevTools (Network + Console) su ogni pagina CRUD per evitare errori silenziosi.
- Chiedere sempre test automatici backend (`dotnet test tests/backend/FilmAPI.Tests.csproj`) dopo refactor di path/routing.
