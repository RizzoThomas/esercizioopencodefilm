# Programmazione Pubblica e Catalogo Film

## Panoramica

La sezione pubblica di CineBase permette agli utenti di esplorare la programmazione cinematografica in modo moderno, con ricerca, filtri, geolocalizzazione e selezione del cinema preferito.

---

## Pagine Coinvolte

| Pagina | File HTML | File JS | Descrizione |
|--------|-----------|---------|-------------|
| Home | `index.html` | `home.js` | Hero cinematico + film in evidenza + raccomandazioni AI |
| Programmazione | `programmazione.html` | `programmazione.js` | Catalogo film-centric con tabs, ricerca, filtri |
| Scheda Film | `scheda-film.html` | `scheda-film.js` | Dettaglio film con rail date e orari show |
| My Cinemas | `my-cinemas.html` | `my-cinemas.js` | Vista cinema-centric con programmazione giornaliera |

---

## Flusso di Navigazione Pubblica

```mermaid
flowchart TD
    HOME[index.html<br/>Home Page] -->|Hero CTA| PROG
    HOME -->|Card film| SCHED
    HOME -->|Raccomandazioni AI| SCHED

    PROG[programmazione.html<br/>Catalogo Film] -->|Seleziona cinema| MODAL[Modale Scelta Cinema]
    MODAL -->|Persistenza| LS[(localStorage / Backend)]
    PROG -->|Tabs: Evidenza/Uscita/Tutti| FILMS[Griglia/Carosello Film]
    PROG -->|Search + Categoria| FILTRATI[Film filtrati]
    PROG -->|Click card| SCHED

    SCHED[scheda-film.html<br/>Dettaglio Film] -->|Rail date| DATE[Selezione data]
    DATE -->|Show per tipo sala| ORARI[Bottoni orario]
    ORARI -->|Autenticato| ACQ[acquista.html]
    ORARI -->|Anonimo| LOGIN[login.html]

    MYC[my-cinemas.html<br/>Cinema-centric] -->|Lista cinema| CARD[Card cinema]
    CARD -->|Click| DETT[Dettaglio cinema]
    DETT -->|Rail date| PROG_DAY[Programmazione giornaliera]
```

---

## Home Page (`index.html`)

### Logica (`home.js`)

```mermaid
flowchart TD
    A[DOMContentLoaded] --> B{URL ha forbidden=true?}
    B -->|Sì| C[Mostra toast 'Non hai permessi']
    B -->|No| D[Rimuovi param forbidden]

    D --> E[loadFeaturedFilms]
    E --> F[API.getFilms + API.getProiezioni in parallelo]
    F --> G[buildFeaturedSelection]
    G --> H[Calcola prossimi 7 giorni]
    H --> I[Conta proiezioni per film]
    I --> J[Ordina: più proiezioni → più recenti]
    J --> K[Prendi top 5 film]

    K --> L[initFeaturedFilms]
    L --> M[Hero card grande + 4 card compatte]
    M --> N[Avvia rotazione automatica ogni 6s]

    O[loadRecommendations] --> P{Auth.isLoggedIn?}
    P -->|No| Q[Esci]
    P -->|Sì| R[GET /recommendations]
    R --> S[Raccomandazioni personalizzate]
    S --> T[Card film con motivo]
```

### API Chiamate
- `API.getFilms({ page: 1, pageSize: 100 })` — tutti i film
- `API.getProiezioni()` — proiezioni per calcolo "In Evidenza"
- `GET /recommendations` — raccomandazioni AI (solo autenticato)

---

## Programmazione (`programmazione.html`)

### Logica (`programmazione.js`)

```mermaid
flowchart TD
    A[DOMContentLoaded] --> B[Carica pendingOffertaId da URL/sessionStorage]
    B --> C[CinemaManager.syncCinemaPreferito]

    C --> D[Imposta selectedCinemaId]
    D --> E[setupTabs, setupSearch, setupCategoriaFilter]
    D --> F[setupCinemaModal, setupCarouselControls, setupLoadMore]

    F --> G[Load iniziale parallelo]
    G --> H1[loadFilms]
    G --> H2[loadCategorie → populateCategoriaFilter]
    G --> H3[loadCinemas → renderCinemaHeader]

    H1 --> I{selectedCinemaId == null?}
    I -->|Sì| J[Mostra no-cinema-state]
    I -->|No| K[Chiama GET /programmazione/films con params]

    K --> L{currentTab?}
    L -->|evidenza/uscita| M[Carosello orizzontale con scroll infinito]
    L -->|tutti| N[Grid 4 colonne con Load More]

    H3 --> O[Geolocalizzazione non bloccante]
    O --> P[Calcolo distanza Haversine]
    P --> Q[Ordina cinema per distanza]

    R[CinemaManager.syncCinemaPreferito] --> S{Autenticato?}
    S -->|Sì| T[GET /profilo/cinema-preferito]
    S -->|No| U[Leggi da localStorage]
    T --> V{Backend ha cinema?}
    V -->|Sì| W[Aggiorna localStorage]
    V -->|No| X{localStorage ha cinema?}
    X -->|Sì| Y[PUT /profilo/cinema-preferito → sync]
```

### CinemaManager

Il `CinemaManager` gestisce la persistenza del cinema selezionato:

```javascript
const CinemaManager = {
  STORAGE_KEY: 'cb_selected_cinema',

  // Anonimo: localStorage
  getLocalCinemaId() { ... },
  setLocalCinemaId(cinemaId) { ... },

  // Autenticato: backend come source of truth
  async syncCinemaPreferito() {
    const auth = getAuthSafe();
    if (!auth || !auth.isLoggedIn()) return this.getLocalCinemaId();

    const backendId = await API.getCinemaPreferito();
    const localId = this.getLocalCinemaId();

    // Sincronizzazione bidirezionale
    if (backendId != null) {
      this.setLocalCinemaId(backendId);
      return backendId;
    }
    if (localId != null) {
      await API.setCinemaPreferito(localId);
      return localId;
    }
    return null;
  },

  async setCinema(cinemaId) {
    this.setLocalCinemaId(cinemaId);
    if (auth?.isLoggedIn()) await API.setCinemaPreferito(cinemaId);
    window.dispatchEvent(new CustomEvent('cinema:changed', { ... }));
  }
};
```

### Tabs e Caroselli

| Tab | Comportamento | Page Size |
|-----|---------------|-----------|
| **In Evidenza** | Film con show nei prossimi 7 giorni. Carosello orizzontale con scroll infinito | 8 film per page |
| **In Uscita** | Film senza show attivi ma con data rilascio futura. Carosello orizzontale | 8 film per page |
| **Tutti i Film** | Grid 4 colonne responsive con paginazione server-side "Carica altri" | 20 film per page |

### API Chiamate
- `API.getProgrammazioneFilms(params)` — `GET /programmazione/films?tab=&search=&categoriaId=&cinemaId=&page=&pageSize=`
- `API.getProgrammazioneCinemas(params)` — `GET /programmazione/cinemas?lat=&lng=`
- `API.getCinemaPreferito()` — `GET /profilo/cinema-preferito`
- `API.setCinemaPreferito(id)` — `PUT /profilo/cinema-preferito/{id}`

---

## Scheda Film (`scheda-film.html`)

### Logica (`scheda-film.js`)

1. Legge `id` e `cinema` dalla query string
2. Carica scheda da `GET /films/{id}/scheda?cinemaId=`
3. Renderizza:
   - **Hero**: copertina, titolo, metadati, descrizione, cast, regista
   - **Rail date orizzontale**: component `date-rail.js` con giorni dinamici
   - **Show raggruppati per TipoSala**: 2D, 3D, ISENSE, XL
   - **Bottoni orario** auth-aware: autenticato → acquista, anonimo → login con redirect
   - **Modale cambio cinema** con sync cinema preferito

```mermaid
flowchart TD
    A[Carica scheda film] --> B[GET /films/{id}/scheda?cinemaId=]
    B --> C{Risposta OK?}
    C -->|No| D[Mostra errore]
    C -->|Sì| E[Render hero: copertina, titolo, cast]
    E --> F[Init date-rail: oggi + 7 giorni]
    F --> G[Seleziona data corrente]
    G --> H[Render show raggruppati per tipo sala]
    H --> I{Utente autenticato?}
    I -->|Sì| J[Bottone → /acquista.html?showId=X]
    I -->|No| K[Bottone → /login.html?redirect=/acquista.html]
```

### API Chiamate
- `API.getFilmScheda(id, cinemaId)` — `GET /films/{id}/scheda?cinemaId=`

---

## My Cinemas (`my-cinemas.html`)

### Logica (`my-cinemas.js`)

Due viste:
1. **Lista cinema**: card con nome, città, indirizzo, tipologie sala, distanza
2. **Dettaglio cinema** (`?IdCinema=X`): header info cinema, rail date, film del giorno con show

### API Chiamate
- `API.getMyCinemas()` — `GET /my-cinemas`
- `API.getCinemaSchedule(cinemaId, date)` — `GET /my-cinemas/{cinemaId}/schedule?date=`

---

## Endpoint Backend Programmazione

| Metodo | Endpoint | Auth | Descrizione |
|--------|----------|------|-------------|
| `GET` | `/programmazione/films` | AllowAnonymous | Listing film con tab, search, categoria, cinemaId, paginato |
| `GET` | `/programmazione/cinemas` | AllowAnonymous | Elenco cinema con ordinamento per distanza (Haversine) |
| `GET` | `/films/{id}/scheda` | AllowAnonymous | Scheda film completa con calendario show |
| `GET` | `/my-cinemas` | AllowAnonymous | Elenco cinema per vista cinema-centric |
| `GET` | `/my-cinemas/{cinemaId}/schedule` | AllowAnonymous | Programmazione giornaliera di un cinema |
| `GET` | `/profilo/cinema-preferito` | Authenticated | Legge cinema preferito |
| `PUT` | `/profilo/cinema-preferito/{cinemaId}` | Authenticated | Imposta cinema preferito |
| `PUT` | `/profilo/cinema-preferito` | Authenticated | Cancella cinema preferito |

### Servizi Backend

| Servizio | Metodi Principali |
|----------|-------------------|
| `IProgrammazioneService` | `GetFilmsAsync`, `GetCinemasAsync`, `GetFilmSchedaAsync`, `GetMyCinemasAsync`, `GetCinemaScheduleAsync` |
| `IProfiloService` | `GetCinemaPreferitoAsync`, `SetCinemaPreferitoAsync` |
| `IFilmService` | CRUD completo film |
| `ICinemaService` | CRUD completo cinema |

### Calcolo Distanza (Haversine)

Il backend calcola la distanza tra l'utente e i cinema usando la formula di Haversine:

```csharp
// Semplificato
double Distance(double lat1, double lon1, double lat2, double lon2) {
    const double R = 6371; // Raggio Terra in km
    double dLat = ToRad(lat2 - lat1);
    double dLon = ToRad(lon2 - lon1);
    double a = Math.Sin(dLat/2) * Math.Sin(dLat/2) +
               Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
               Math.Sin(dLon/2) * Math.Sin(dLon/2);
    double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a));
    return R * c;
}
```
