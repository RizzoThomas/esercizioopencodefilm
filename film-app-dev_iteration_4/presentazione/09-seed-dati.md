# Seed Dati Realistico

## Panoramica

CineBase include un progetto console standalone (`FilmApiSeeder`) per popolare il database con dati reali e credibili, utilizzando l'API TMDB per film, copertine e cast.

---

## Progetto FilmApiSeeder

```
backend/scripts/FilmApiSeeder/
├── FilmApiSeeder.csproj    # Progetto console .NET
├── Program.cs              # Orchestratore seed
├── TmdbClient.cs           # Client API TMDB
└── SeedCatalog.cs          # Catalogo dati seed
```

### Dipendenze
- Riferisce `FilmAPI.csproj` per modelli e DbContext
- Legge configurazione da `backend/.env`
- Usa TMDB API v3 per dati film

---

## Comandi Disponibili

```bash
# Seed completo (aggiunge senza cancellare)
dotnet run --project backend/scripts/FilmApiSeeder

# Reset e reseed della sola programmazione
dotnet run --project backend/scripts/FilmApiSeeder -- --reset-shows --force

# Reset completo e reseed (tutto)
dotnet run --project backend/scripts/FilmApiSeeder -- --reset-all --force
```

---

## Flusso di Seed

```mermaid
flowchart TD
    START[Avvio FilmApiSeeder] --> CFG[Leggi backend/.env]

    CFG --> CAT[Seed Categorie]
    CAT --> REG[Seed Registi]
    REG -->[+ TMDB] FILM[Seed Film da TMDB]
    FILM -->[+ TMDB] UPD[Aggiorna dettagli: cast, copertine, voto]

    UPD --> CIN[Seed Cinema italiani]
    CIN --> SAL[Seed Sale multi-tipologia]
    SAL --> POSTI[Genera piantine posti per ogni sala]
    POSTI --> SHOW[Genera programmazione show]

    SHOW --> VER{Verifica}
    VER --> OK[Seed completato]
    VER --> FAIL[Mostra errori]

    TMDB[TMDB API] -.->|Ricerca film| FILM
    TMDB -.->|Dettagli + crediti| UPD
```

---

## Integrazione TMDB

### `TmdbClient.cs`

```csharp
public class TmdbClient {
    // Ricerca film per titolo
    Task<List<TmdbMovie>> SearchMovieAsync(string query);
    
    // Dettaglio film con cast
    Task<TmdbMovieDetail> GetMovieDetailAsync(int tmdbId);
    
    // URL copertine
    string GetPosterUrl(string path, string size = "w500");
    string GetBackdropUrl(string path, string size = "w1280");
}
```

### Dati Importati da TMDB

| Campo | Fonte TMDB |
|-------|------------|
| `Titolo` | `original_title` |
| `DescrizioneLunga` | `overview` |
| `CastText` | `credits.cast[].name` |
| `DataRilascio` | `release_date` |
| `CopertinaPath` | `poster_path` | 
| `BackdropPath` | `backdrop_path` |
| `VoteAverage` | `vote_average` |
| `VoteCount` | `vote_count` |
| `Popularity` | `popularity` |
| `TmdbId` | `id` |
| `ImdbId` | `imdb_id` |
| `OriginalLanguage` | `original_language` |
| `Regista` | `credits.crew[].job=Director` |

---

## Dati Seedati

### Categorie (generiche)
```
Azione, Commedia, Drammatico, Horror, Fantascienza, Thriller,
Animazione, Documentario, Avventura, Romantico, Musicale, Western
```

### Film (64 film — esempio)

| Titolo | TMDB | Categorie |
|--------|------|-----------|
| Il Padrino | tmdb | Drammatico |
| Interstellar | tmdb | Fantascienza, Avventura |
| Pulp Fiction | tmdb | Thriller |
| ... (61 altri film) | ... | ... |

### Cinema Italiani (20)

| Cinema | Città | Coordinate |
|--------|-------|------------|
| Roma Moderno | Roma | 41.9028, 12.4964 |
| Milano Duomo | Milano | 45.4642, 9.1900 |
| Napoli Centro | Napoli | 40.8518, 14.2681 |
| ... (17 altri cinema) | ... | ... |

### Sale per Cinema (83 totali)

Ogni cinema ha 3-5 sale di tipologie diverse:

| Tipo Sala | Posti | Supplemento |
|-----------|-------|-------------|
| 2D | 80-150 | €0.00 |
| 3D | 80-120 | €2.00 |
| ISENSE | 60-100 | €4.00 |
| XL | 100-200 | €3.00 |

### Piantine Posti

Ogni sala ha posti generati con:
- **Settori**: Platea-Centro, Platea-SX, Platea-DX, Galleria (opzionale)
- **File**: numerate da 1 a N
- **Posti per fila**: da 6 a 15
- **Posti wheelchair**: 1-3 per settore
- **Coordinate**: PosX, PosY per rendering sulla mappa

### Programmazione Show

- Show generati per i prossimi 7-14 giorni
- Prezzi: `DEFAULT_TICKET_PRICE` (default €8.50) + supplemento sala
- Durata: snapshot dalla durata del film
- Evitati overlap nella stessa sala
- Film distribuiti uniformemente tra i cinema

---

## Configurazione `.env`

```env
# Database
DB_CONNECTION_STRING=Server=localhost;Database=cinebase;...

# TMDB (per seeder)
TMDB_BEARER_TOKEN=eyJhbGciOiJIUzI1NiJ9...

# Ticketing
DEFAULT_TICKET_PRICE=8.50
HOLD_TTL_MINUTES=10
MAX_SEATS_PER_ORDER=10

# Stripe
STRIPE_API_KEY=sk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...
```
