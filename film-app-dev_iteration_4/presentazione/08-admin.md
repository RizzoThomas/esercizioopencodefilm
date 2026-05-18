# Pannello Amministrativo

## Panoramica

L'area admin di CineBase offre una shell unificata per tutte le operazioni di gestione: CRUD di film, registi, sale, show, categorie, utenti e credito.

---

## Architettura Admin

```mermaid
graph TB
    subgraph "Frontend Admin"
        SHELL[admin-shell.js]
        DASH[dashboard.html]
        FILMS[films.html]
        REGISTI[registi.html]
        CINEMAS[cinemas.html]
        PROIEZ[proiezioni.html]
        CAT[categorie.html]
        SALE[sale.html]  <!-- non ancora implementata -->
        UTENTI[utenti.html]
        UT_DET[utenti-detail.html]
    end

    subgraph "Backend"
        FE[FilmsEndpoints]
        RE[RegistiEndpoints]
        CE[CinemasEndpoints]
        PE[ProiezioniEndpoints]
        CAE[CategorieEndpoints]
        SE[ShowsEndpoints]
        SAE[SaleEndpoints]
        AUE[AdminUtentiEndpoints]
        CRE[CreditoEndpoints]
    end

    subgraph "Servizi"
        FS[FilmService]
        RS[RegistaService]
        CS[CinemaService]
        PRS[ProiezioneService]
        CAS[CategoriaService]
        SHS[ShowService]
        SAS[SalaService]
        AUS[UserAdminService]
        CRS[CreditoService]
    end

    SHELL --> DASH
    SHELL --> FILMS
    SHELL --> REGISTI
    SHELL --> CINEMAS
    SHELL --> PROIEZ
    SHELL --> CAT

    FILMS --> FE --> FS
    REGISTI --> RE --> RS
    CINEMAS --> CE --> CS
    PROIEZ --> PE --> PRS
    CAT --> CAE --> CAS
    SALE --> SAE --> SAS
    PROIEZ --> SE --> SHS
    UTENTI --> AUE --> AUS
```

---

## Shell Admin Unificata (`admin-shell.js`)

La shell admin fornisce:
- **Sidebar laterale** fissa con tutte le voci admin
- **Topbar secondaria** con menu utente (profilo, logout)
- **Gestione link attivo** (highlight della pagina corrente)
- **Toggle sidebar mobile** (hamburger)
- **Cambio tema** (light/dark) dalla sidebar

```html
<!-- Sidebar esempio -->
<div class="admin-sidebar">
  <nav>
    <a href="/dashboard.html">📊 Dashboard</a>
    <a href="/films.html">🎬 Film</a>
    <a href="/registi.html">🎭 Registi</a>
    <a href="/cinemas.html">🏢 Cinema</a>
    <a href="/proiezioni.html">📅 Proiezioni</a>
    <a href="/categorie.html">🏷️ Categorie</a>
  </nav>
  <div class="admin-sidebar-footer">
    <button onclick="toggleTheme()">🌓 Cambia tema</button>
  </div>
</div>
```

Il `template-loader.js` esclude il caricamento della navbar/footer legacy sulle pagine che usano la shell admin.

---

## Pagine Admin e Loro Logica

### Dashboard (`dashboard.html`)

Riepilogo con stato delle proiezioni recenti usando badge `chip-status`:

- Proiezioni "Passata" / "In programma"
- Collegamenti rapidi a tutte le CRUD

### Film (`films.html`) — `films.js`

```mermaid
flowchart TD
    A[Carica pagina] --> B[GET /films con paginazione + search]
    B --> C[Render tabella: ID, Titolo, Durata, Categorie, Regista, Azioni]
    
    C --> D{Creazione/Modifica}
    D --> E[Form: titolo, durata, regista, categorie, descrizione, copertina]
    E --> F[POST/PUT /films]
    F --> G[Refresh tabella]

    C --> H{Elimina}
    H --> I[DELETE /films/{id}]
    I --> G
```

- **Ricerca** per titolo con paginazione server-side
- **Categorie**: gruppo checkbox con categorie dinamiche
- **Regista**: dropdown con lista registi
- **TMDB Search**: integrazione per importare dati film da TMDB

### Registi (`registi.html`) — `registi.js`

CRUD completo con:
- Tabella: Nome, Cognome, Azioni
- Paginazione con controlli first/prev/next/last
- Modal Bootstrap per creazione/modifica

### Cinema (`cinemas.html`) — `cinemas.js`

Gestione cinema con:
- Barra ricerca testuale + paginazione server-side
- Card/Lista: Nome, Città, Indirizzo, Coordinate, Azioni
- Form: Nome, Città, Indirizzo, Latitudine, Longitudine

### Proiezioni/Schedule (`proiezioni.html`) — `proiezioni.js`

Bridge legacy tra vecchio modello `Proiezione` e nuovo `Show`:
- Tabella: Film, Cinema, Sala, Data, Ora, Stato
- Stato calcolato: "Passata" / "In programma" (non più hardcoded)
- Ricerca + paginazione server-side
- Creazione show con validazione anti-overlap

### Categorie (`categorie.html`) — `categorie.js`

CRUD semplice per categorie film:
- Lista badge categorie
- Create, Update, Delete con feedback UI (toast)
- Relazione many-to-many con Film

### Utenti (`utenti.html`) — `utenti.js` + `utenti-detail.js`

Solo Admin:
- Lista utenti con ricerca
- Dettaglio utente: profilo, ruolo, credito, stato
- Ricarica credito admin
- Disabilita/Abilita account
- Reset password

### Validazione Biglietti (`validazione.html`) — `validazione.js`

Interfaccia per operatori cinema:
- Input codice biglietto (o scansione QR)
- Dettaglio biglietto: film, cinema, sala, data, posto
- Pulsante "Valida" per conferma ingresso

---

## Endpoint Backend Admin

| Metodo | Endpoint | Auth | Descrizione |
|--------|----------|------|-------------|
| `GET` | `/films` | AllowAnonymous | Lista film (paginata, con search) |
| `POST` | `/films` | PowerUserOrAdmin | Crea film |
| `PUT` | `/films/{id}` | PowerUserOrAdmin | Aggiorna film |
| `DELETE` | `/films/{id}` | PowerUserOrAdmin | Elimina film |
| `GET` | `/registi` | AllowAnonymous | Lista registi (paginata) |
| `POST` | `/registi` | PowerUserOrAdmin | Crea regista |
| `GET` | `/cinemas` | AllowAnonymous | Lista cinema (paginata) |
| `POST` | `/cinemas` | PowerUserOrAdmin | Crea cinema |
| `DELETE` | `/cinemas/{id}` | AdminOnly | Elimina cinema |
| `GET` | `/proiezioni` | AllowAnonymous | Lista proiezioni (paginata) |
| `POST` | `/proiezioni` | PowerUserOrAdmin | Crea proiezione (bridge legacy → Show) |
| `GET` | `/shows` | AllowAnonymous | Lista show |
| `POST` | `/shows` | PowerUserOrAdmin | Crea show |
| `PUT` | `/shows/{id}` | PowerUserOrAdmin | Aggiorna show |
| `DELETE` | `/shows/{id}` | PowerUserOrAdmin | Elimina show |
| `GET` | `/cinemas/{cinemaId}/sale` | AllowAnonymous | Lista sale per cinema |
| `POST` | `/cinemas/{cinemaId}/sale` | PowerUserOrAdmin | Crea sala |
| `PUT` | `/sale/{salaId}/posti` | PowerUserOrAdmin | Salva piantina posti |
| `GET` | `/categorie` | AllowAnonymous | Lista categorie |
| `POST` | `/categorie` | PowerUserOrAdmin | Crea categoria |
| `GET` | `/admin/utenti` | AdminOnly | Lista utenti |
| `PUT` | `/admin/utenti/{id}/ruolo` | AdminOnly | Cambia ruolo |
| `POST` | `/admin/credito/ricarica` | AdminOnly | Ricarica credito |

---

## Servizi Backend Admin

| Servizio | Metodi Principali |
|----------|-------------------|
| `IFilmService` | `GetAllAsync`, `GetPagedAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync` |
| `IRegistaService` | `GetAllAsync`, `GetPagedAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync` |
| `ICinemaService` | `GetAllAsync`, `GetPagedAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync` |
| `IProiezioneService` | Bridge legacy → Show |
| `IShowService` | CRUD con validazione anti-overlap |
| `ISalaService` | CRUD sale + gestione piantina posti |
| `ICategoriaService` | CRUD categorie |
| `IUserAdminService` | Gestione utenti, ruoli, credito |

---

## Paginazione Server-Side

Tre endpoint supportano paginazione e ricerca:

```csharp
// Esempio: RegistiEndpoints
app.MapGet("/registi", async (int? page, int? pageSize, string? search) => {
    if (page.HasValue && pageSize.HasValue) {
        return Results.Ok(await _service.GetPagedAsync(page.Value, pageSize.Value, search));
    }
    // Fallback legacy: lista completa (array)
    return Results.Ok(await _service.GetAllAsync());
});
```

DTO paginati:
```csharp
public class RegistaPagedResultDTO {
    public List<RegistaDTO> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage { get; set; }
}
```
