# Architettura del Sistema

## Stack Tecnologico Dettagliato

| Livello | Tecnologia | Versione | Ruolo |
|---------|-----------|----------|-------|
| **Backend** | .NET (C#) | 8.0 | API REST, business logic, ORM |
| **Backend** | ASP.NET Core | 8.0 | Web framework, middleware pipeline |
| **Backend** | Entity Framework Core | 8.0 | ORM per MySQL |
| **Database** | MySQL | 8.0 | Database relazionale |
| **Frontend** | HTML5 + JavaScript ES6 | - | Single-page application |
| **Frontend** | Tailwind CSS | 3.x | Utility-first CSS framework |
| **Frontend** | Font Awesome | 6.x | Icone |
| **Pagamenti** | Stripe .NET SDK | - | Stripe Checkout, webhook |
| **Pagamenti** | Stripe.js | - | Frontend Stripe Checkout |
| **Ticketing** | QuestPDF | - | Generazione PDF multipagina |
| **Ticketing** | QRCoder | - | Generazione QR code |
| **Ticketing** | ZXing.Net | - | Barcode grafico |
| **Ticketing** | MailKit | - | Invio email SMTP |
| **Ticketing** | PdfPig | - | Lettura PDF nei test |
| **Design** | Google Stitch | - | Design system tokens |
| **Seed** | TMDB API | - | Dati film realistici |

---

## Architettura Generale

```mermaid
graph TB
    subgraph "Frontend (Browser)"
        HTML[Pagine HTML]
        JS[JavaScript ES6]
        CSS[Tailwind CSS + Custom]
        AUTH[auth.js - JWT Management]
        API[api.js - HTTP Client]
        ROUTE[route-guard.js - RBAC Frontend]
        COMP[Componenti: navbar, footer]
    end

    subgraph "Backend (ASP.NET Core)"
        API_END[Endpoints REST]
        SVC[Services - Business Logic]
        DTO[DTO - Data Transfer Objects]
        MODEL[Entity Models]
        DBCTX[FilmDbContext - EF Core]
        MIDDL[Middleware: JWT, CORS, Rate Limiting]
    end

    subgraph "Storage"
        MYSQL[(MySQL Database)]
        ENV[.env Config]
    end

    subgraph "Esterni"
        STRIPE[Stripe API]
        TMDB[TMDB API]
        SMTP[SMTP Server]
    end

    HTML --> JS
    JS --> API
    API --> API_END
    AUTH --> ROUTE
    ROUTE --> AUTH
    COMP --> HTML

    API_END --> MIDDL
    MIDDL --> SVC
    SVC --> DTO
    SVC --> DBCTX
    DBCTX --> MODEL
    DBCTX --> MYSQL

    SVC --> STRIPE
    SVC --> TMDB
    SVC --> SMTP
    SVC --> ENV
```

---

## Pipeline Richieste HTTP

```mermaid
sequenceDiagram
    participant Browser as Browser
    participant CDN as CDN (Tailwind/FontAwesome)
    participant RG as route-guard.js
    participant Auth as auth.js
    participant API as api.js
    participant BE as Backend ASP.NET
    participant DB as MySQL

    Browser->>CDN: Carica CSS/JS
    Browser->>RG: Esegui route-guard (IIFE)
    RG->>RG: Legge token da localStorage
    RG->>RG: Verifica PAGE_PERMISSIONS[path]
    alt Ruolo non autorizzato
        RG->>Browser: Redirect a login/index (replace)
    else Ruolo autorizzato
        RG->>Browser: Permetti rendering pagina
    end

    Browser->>Auth: init Auth
    Auth->>Auth: Carica token, deviceId
    Auth->>Auth: isLoggedIn() → parse JWT, check exp

    Browser->>API: Chiamata endpoint
    API->>API: apiFetch(url, options)
    API->>BE: HTTP Request + Bearer Token
    BE->>BE: Middleware JWT → valida token
    BE->>DB: Query/Command
    DB-->>BE: Risultato
    BE-->>API: JSON Response
    alt 401 Unauthorized
        API->>Auth: refreshToken()
        Auth->>BE: POST /auth/refresh + deviceId
        BE-->>Auth: Nuovo access token
        Auth->>API: Retry chiamata originale
    end
    API-->>Browser: Dati renderizzati
```

---

## Struttura dei Progetti

```
film-app-dev_iteration_4/
├── backend/
│   ├── FilmAPI/                     # API principale
│   │   ├── Model/                   # Entità EF Core (37 file)
│   │   ├── DTO/                     # Data Transfer Objects (21 file)
│   │   ├── Services/                # Business logic (55 file)
│   │   ├── Endpoints/               # REST API endpoints (29 file)
│   │   ├── Data/                    # DbContext, Migration, Seeder
│   │   └── Migrations/              # EF Core migrations
│   └── scripts/
│       └── FilmApiSeeder/           # Seeder standalone
│
├── frontend/
│   └── CineBase.Web/
│       └── wwwroot/
│           ├── *.html               # 34 pagine HTML
│           ├── js/
│           │   ├── auth.js          # Gestione autenticazione
│           │   ├── api.js           # Client HTTP (617 lines)
│           │   ├── route-guard.js   # Route guard RBAC
│           │   ├── theme.js         # Tema light/dark
│           │   ├── template-loader.js # Caricamento componenti
│           │   ├── date-rail.js     # Componente rail date
│           │   └── pages/           # 23 page-specific JS
│           ├── css/
│           │   └── styles.css       # Design system tokens
│           └── components/
│               ├── navbar-landing.html
│               ├── navbar-admin.html (legacy)
│               ├── footer-landing.html
│               └── footer-admin.html
│
├── tests/
│   └── backend/
│       └── FilmAPI.Tests/
│           ├── Integration/         # Test integrazione
│           └── Unit/                # Test unitari
│
├── docs/                            # Documentazione
└── presentazione/                   #
```

---

## Design System (Ferrari-inspired)

Il design system trae ispirazione dal linguaggio visivo Ferrari con:

- **Canvas**: near-black `#181818` — mai nero puro
- **Accento primario**: Rosso Corsa `#da291c` — usato con parsimonia
- **Tipografia**: FerrariSans / Inter, display weight 500
- **Angoli**: sharp `0px` su tutti i CTA e card
- **CTA**: uppercase con tracking 1.4px
- **Spaziatura**: scala 8px (4/8/16/24/32/48/64/96/128)
- **Elevazione**: profondità fotografica, nessun drop shadow
- **Tema**: supporto completo light/dark mode

Vedi `DESIGN.md` per la specifica completa dei token di design.
