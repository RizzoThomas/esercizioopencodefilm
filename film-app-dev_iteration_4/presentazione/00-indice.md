# CineBase

> **Piattaforma Web per la Gestione di Cinema Multisala e Ticketing Digitale**
> Versione: Iterazione 4 | Stack: .NET + MySQL + Tailwind CSS + Stripe

---

## Indice dei Documenti

| # | Documento | Descrizione |
|---|-----------|-------------|
| **00** | `00-indice.md` | **Questo file** — indice generale e legenda |
| **01** | `01-architettura.md` | Architettura del sistema: stack, diagramma componenti, flusso richieste |
| **02** | `02-modello-dati.md` | Modello dati: entità, relazioni, enumerazioni, migrations EF |
| **03** | `03-autenticazione.md` | Sistema di autenticazione: JWT, refresh token device-aware, RBAC, 2FA, social login |
| **04** | `04-programmazione.md` | Catalogo pubblico: programmazione film, scheda film, cinema preferito, geolocalizzazione |
| **05** | `05-acquisto-biglietti.md` | Flusso acquisto: seat-map interattiva, hold posti, countdown, pagamento |
| **06** | `06-pagamenti.md` | Pagamenti: Stripe Checkout, credito piattaforma, pagamento misto, webhook |
| **07** | `07-ticketing.md` | Ticketing digitale: emissione ticket, PDF, email, validazione QR |
| **08** | `08-admin.md` | Pannello admin: CRUD, shell unificata, paginazione server-side, utenti |
| **09** | `09-seed-dati.md` | Seed realistico: FilmApiSeeder, TMDB, 64 film, 20 cinema |
| **10** | `10-test.md` | Test suite: 231 test integrazione, copertura, fakes |

---

## Legenda Diagrammi Mermaid

Tutti i diagrammi in questi documenti usano [Mermaid](https://mermaid.js.org/) e sono visualizzabili in qualsiasi Markdown viewer che supporti Mermaid (GitHub, VS Code con estensione, ecc.).

### Tipi di diagramma usati:
- **`flowchart TD`** — Diagrammi di flusso (processi, sequenze operative)
- **`graph LR`** — Grafi relazionali (architettura, dipendenze)
- **`classDiagram`** — Diagrammi UML delle classi/entità
- **`sequenceDiagram`** — Sequenze temporali (interazioni API)
- **`stateDiagram-v2`** — Macchine a stati (cicli di vita entità)

---

## Stack Tecnologico Riepilogativo

```
┌─────────────────────────────────────────────────────────────┐
│                    CineBase Platform                         │
├─────────────────┬───────────────────┬───────────────────────┤
│   Frontend      │   Backend         │   Infrastruttura      │
├─────────────────┼───────────────────┼───────────────────────┤
│ HTML5           │ .NET 8 (C#)       │ MySQL 8               │
│ Tailwind CSS    │ ASP.NET Core      │ Entity Framework Core │
│ JavaScript ES6  │ REST API          │ Stripe Checkout       │
│ Font Awesome 6  │ JWT Auth          │ Google SMTP           │
│ Stripe.js       │ Swagger/OpenAPI   │ Twilio SendGrid       │
│ Stitch (design) │ QuestPDF          │ TMDB API              │
└─────────────────┴───────────────────┴───────────────────────┘
```
