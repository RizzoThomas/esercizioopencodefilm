# Piano di Lavoro - Iterazione 3 (Ottimizzato)

Autore: OpenCode con GPT-5.3-Codex fondendo i piani di lavoro fatti con Opus 4.6 e MiMo V2 Pro High

## Stato Avanzamento Fasi

| Fase | Stato | Data | Note |
| --- | --- | --- | --- |
| FASE 1 - Modello Dati, JWT Infrastructure, Migration e Seed | Completata | 2026-04-06 | Migration applicata, seed admin + 12 categorie OK, 71/71 test verdi |
| FASE 2 - Categorie e aggiornamento Film per many-to-many | Completata | 2026-04-06 | CRUD categorie OK, film con categorie multiple, 71/71 test verdi |
| FASE 3 - Auth Service e endpoint autenticazione | Completata | 2026-04-06 | Register/login/refresh/logout/me funzionanti, JWT con rotation. Bug fix critico: aggiunto SaveChangesAsync mancante in RegisterAsync/LoginAsync. 71/71 test verdi |
| FASE 4 - Enforcement RBAC globale su tutte le API | Completata | 2026-04-06 | Middleware auth attivi, policy RBAC applicate su tutti gli endpoint secondo matrice, fix parsing claim `sub` su `/auth/me`, verifica manuale 200/401/403 OK, 71/71 test verdi |
| FASE 5 - Area Personale, Prenotazioni, Gestione Utenti Admin | Completata | 2026-04-06 | DTO/servizi/endpoint implementati; verifica manuale completa OK (isolamento profilo/prenotazioni, admin vede tutte prenotazioni, update ruoli, blocco downgrade ultimo admin); 71/71 test verdi |
| FASE 6 - Aggiornamento e ampliamento test backend | Completata | 2026-04-06 | 97/97 test verdi: A1-A8, RB1-RB8, CAT1-CAT5, PR1-PR5 + test esistenti |
| FASE 7 - Frontend Auth reale e token lifecycle | Completata | 2026-04-06 | auth.js + api.js con Bearer/refresh, login/register/logout end-to-end OK, fix UI auth pages/navbar e hardening accesso area admin lato frontend, 97/97 test verdi |
| FASE 8 - Route guard e navigazione per ruolo | Completata | 2026-04-07 | route-guard.js creato, navbar aggiornate, redirect verificati, 97/97 test verdi |
| FASE 9 - Programmazione pubblica + gestione categorie admin | Completata | 2026-04-12 | `programmazione.html` pubblica con filtri operativi; CRUD categorie power/admin; home riprogettata in ottica discovery con sezione featured (hero + mini-grid) e CTA verso programmazione; categorie visibili in admin e landing |
| FASE 10 - Area Personale utente (profilo + prenotazioni) | Completata | 2026-04-12 | `profilo.html`/`profilo.js` creati con update profilo + CRUD prenotazioni + `?prenota=`; refinements UX/accessibilita area profilo e listing admin con paginazione+ricerca; paginazione backend aggiunta per `registi/cinemas/proiezioni`; test backend estesi a 103/103 PASS |
| FASE 11 - Verifica finale, hardening e documentazione | Completata | 2026-04-12 | 103/103 test verdi, RBAC e redirect verificati, documentazione allineata |

## 1) Obiettivo Iterazione

Mettere in sicurezza il backend CineBase e completare il flusso auth end-to-end introducendo:

- autenticazione JWT con **access token + refresh token**
- autorizzazione **RBAC** con ruoli `Admin`, `PowerUser`, `User`, `Anonimo`
- area personale utente con dati contatto e **prenotazioni virtuali**
- protezione pagine frontend con **redirect automatici** in base a login/ruolo
- supporto **categorie film** con relazione many-to-many (un film puo appartenere a piu categorie)

## 1.1 Contesto attuale (da `status.md` e `changelog.md`)

- Iterazione 2.2 completata; backend stabile con `71/71` test verdi
- API backend attualmente aperte (nessuna auth/role enforcement)
- Frontend con autenticazione mock (`sessionStorage`) da sostituire
- Backend .NET 9 Minimal API in `backend/FilmAPI` (porta 5000)
- Frontend statico in `frontend/CineBase.Web` (porta 5001)

## 1.2 Architettura repository

```text
repo-root/
|- backend/FilmAPI/          (API .NET 9)
|- frontend/CineBase.Web/    (MPA statico)
|- tests/backend/            (xUnit + integration)
`- docs/
```

---

## 2) Ruoli, Permessi e Redirect

## 2.1 Definizione ruoli

| Ruolo | Enum | Descrizione |
| --- | --- | --- |
| Admin | `2` | Massimo privilegio: CRUD su tutto, gestione utenti/ruoli, accesso completo area admin |
| PowerUser | `1` | CRUD su Film, Proiezioni, Registi, Categorie; su Cinema solo Read |
| User | `0` | Utente autenticato: programmazione, profilo, prenotazioni virtuali; niente area admin |
| Anonimo | - | Non autenticato: accesso pubblico a `index.html` e `programmazione.html` in sola lettura |

## 2.2 Matrice permessi API (dettaglio endpoint)

| Endpoint | Anonimo | User | PowerUser | Admin |
| --- | --- | --- | --- | --- |
| `POST /auth/register` | SI | - | - | - |
| `POST /auth/login` | SI | - | - | - |
| `POST /auth/refresh` | SI | - | - | - |
| `POST /auth/logout` | - | SI | SI | SI |
| `GET /auth/me` | - | SI | SI | SI |
| `GET /categorie` | SI | SI | SI | SI |
| `GET /categorie/{id}` | SI | SI | SI | SI |
| `POST /categorie` | - | - | SI | SI |
| `PUT /categorie/{id}` | - | - | SI | SI |
| `DELETE /categorie/{id}` | - | - | SI | SI |
| `GET /films` | SI | SI | SI | SI |
| `GET /films/{id}` | SI | SI | SI | SI |
| `POST /films` | - | - | SI | SI |
| `PUT /films/{id}` | - | - | SI | SI |
| `DELETE /films/{id}` | - | - | SI | SI |
| `GET /registi` | - | - | SI | SI |
| `GET /registi/{id}` | - | - | SI | SI |
| `POST /registi` | - | - | SI | SI |
| `PUT /registi/{id}` | - | - | SI | SI |
| `DELETE /registi/{id}` | - | - | SI | SI |
| `GET /registi/{id}/films` | - | - | SI | SI |
| `POST /registi/{id}/films` | - | - | SI | SI |
| `GET /cinemas` | SI | SI | SI | SI |
| `GET /cinemas/{id}` | SI | SI | SI | SI |
| `POST /cinemas` | - | - | - | SI |
| `PUT /cinemas/{id}` | - | - | - | SI |
| `DELETE /cinemas/{id}` | - | - | - | SI |
| `GET /proiezioni` | SI | SI | SI | SI |
| `GET /proiezioni/{id}` | SI | SI | SI | SI |
| `POST /proiezioni` | - | - | SI | SI |
| `PUT /proiezioni/{id}` | - | - | SI | SI |
| `DELETE /proiezioni/{id}` | - | - | SI | SI |
| `POST /media/covers` | - | - | SI | SI |
| `GET /profilo` | - | SI | SI | SI |
| `PUT /profilo` | - | SI | SI | SI |
| `GET /prenotazioni` | - | SI (proprie) | - | SI (tutte) |
| `POST /prenotazioni` | - | SI | - | SI |
| `DELETE /prenotazioni/{id}` | - | SI (proprie) | - | SI |
| `GET /admin/utenti` | - | - | - | SI |
| `PUT /admin/utenti/{id}/ruolo` | - | - | - | SI |

Note:

- endpoint pubblici per navigazione anonima: `GET /films`, `GET /cinemas`, `GET /proiezioni`, `GET /categorie`
- `POST /prenotazioni` consentito a utente autenticato (User/Admin), con ownership e validazioni nel service

## 2.3 Matrice permessi pagine frontend

| Pagina | Anonimo | User | PowerUser | Admin |
| --- | --- | --- | --- | --- |
| `index.html` | SI | SI | SI | SI |
| `programmazione.html` (nuova) | SI | SI | SI | SI |
| `login.html` (nuova) | SI | - | - | - |
| `registrazione.html` (nuova) | SI | - | - | - |
| `profilo.html` (nuova) | - | SI | SI | SI |
| `dashboard.html` | - | - | SI | SI |
| `films.html` | - | - | SI | SI |
| `registi.html` | - | - | SI | SI |
| `cinemas.html` | - | - | SI | SI |
| `proiezioni.html` | - | - | SI | SI |
| `categorie.html` (nuova) | - | - | SI | SI |

## 2.4 Regole di redirect obbligatorie

- utente non loggato su pagina protetta -> redirect `login.html?redirect=<pagina>`
- utente loggato senza ruolo sufficiente -> redirect `index.html` (oppure `profilo.html` se gia autenticato)
- utente loggato che apre `login.html` o `registrazione.html` -> redirect `index.html`
- utente anonimo che clicca "Prenota" -> redirect login; dopo login ritorno alla pagina richiesta

---

## 3) Design Tecnico

## 3.1 Nuove entita

- **Categoria**

   ```text
   Categoria(
   Id int PK,
   Nome string required unique max 100
   )
   Navigation: ICollection<FilmCategoria>
   ```

- **FilmCategoria**

   ```text
   FilmCategoria(
   FilmId int FK,
   CategoriaId int FK
   ) -- PK composita (FilmId, CategoriaId)
   Navigation: Film, Categoria
   ```

- **UserRole**

   ```text
   User = 0, PowerUser = 1, Admin = 2
   ```

- **User**

   ```text
   User(
   Id int PK,
   Email string required unique,
   PasswordHash string required,
   Nome string required max 100,
   Cognome string required max 100,
   Telefono string? max 20,
   Ruolo UserRole required,
   DataRegistrazione DateTime required
   )
   Navigation: ICollection<RefreshToken>, ICollection<Prenotazione>
   ```

- **RefreshToken**

   ```text
   RefreshToken(
   Id int PK,
   Token string required unique,
   UserId int FK,
   ExpiresAt DateTime required,
   CreatedAt DateTime required,
   RevokedAt DateTime?
   )
   Computed: IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow
   ```

- **Prenotazione**

   ```text
   Prenotazione(
   Id int PK,
   UserId int FK,
   ProiezioneId int FK,
   NumeroPosti int required,
   Note string? max 500,
   DataPrenotazione DateTime required
   )
   ```

## 3.2 Modifiche entita esistenti

- `Film`: aggiungere `ICollection<FilmCategoria> FilmCategorie`

## 3.3 Relazioni e vincoli

- Film <-> Categoria many-to-many via `FilmCategoria`
- User 1-N RefreshToken (cascade delete)
- User 1-N Prenotazione (cascade delete)
- Proiezione 1-N Prenotazione (restrict)
- unique index: `Categoria.Nome`, `User.Email`, `RefreshToken.Token`

## 3.4 JWT design

| Parametro | Access Token | Refresh Token |
| --- | --- | --- |
| Formato | JWT HS256 | stringa opaca GUID/random |
| Durata | 15 minuti | 7 giorni |
| Claims | `sub`, `email`, `role`, `nome` | nessun claim |
| Storage frontend | `localStorage` | `localStorage` |
| Rinnovo | endpoint refresh | rotazione (revoca vecchio + nuovo token) |

## 3.5 NuGet packages

- `Microsoft.AspNetCore.Authentication.JwtBearer` (9.x)
- `BCrypt.Net-Next`

## 3.6 Variabili environment da aggiungere

```env
JWT_SECRET=<chiave segreta minimo 256 bit>
JWT_ISSUER=CineBaseAPI
JWT_AUDIENCE=CineBaseWeb
JWT_ACCESS_TOKEN_EXPIRY_MINUTES=15
JWT_REFRESH_TOKEN_EXPIRY_DAYS=7
ADMIN_SEED_EMAIL=admin@cinebase.it
ADMIN_SEED_PASSWORD=Admin123!
```

## 3.7 Seed categorie iniziali

```text
Drammatico, Commedia, Avventura, Fantasy, Horror, Azione,
Fantascienza, Thriller, Animazione, Documentario, Romantico, Storico
```

---

## 4) Fasi di Implementazione (incrementale)

### FASE 1 - Modello Dati, JWT Infrastructure, Migration e Seed

**Obiettivo**: introdurre tutte le nuove entita e la base infrastrutturale auth senza attivare ancora enforcement globale.

**Attivita**:

1. Installare i package NuGet richiesti.
2. Creare file modello:
   - `Model/UserRole.cs`
   - `Model/User.cs`
   - `Model/RefreshToken.cs`
   - `Model/Prenotazione.cs`
   - `Model/Categoria.cs`
   - `Model/FilmCategoria.cs`
3. Aggiornare `Model/Film.cs` con navigation `FilmCategorie`.
4. Aggiornare `Data/FilmDbContext.cs`:
   - nuovi `DbSet`: `Users`, `RefreshTokens`, `Prenotazioni`, `Categorie`, `FilmCategorie`
   - PK composita `FilmCategoria(FilmId, CategoriaId)`
   - indici unici su `Categoria.Nome`, `User.Email`, `RefreshToken.Token`
   - relazioni e delete behaviors come da sezione 3.3
5. Aggiornare `backend/.env` e `backend/.env.example`.
6. Configurare JWT in `Program.cs` (`AddAuthentication().AddJwtBearer(...)`) leggendo env.
7. NON abilitare ancora `UseAuthentication()`/`UseAuthorization()`.
8. Creare migration: `dotnet ef migrations add AddCategorieAndAuth`.
9. Implementare seed automatico:
   - admin da env se tabella utenti vuota
   - categorie iniziali se tabella categorie vuota
10. Applicare migration e verificare dati seed.

**Verifica fase**:

- migration applicata correttamente
- admin presente
- 12 categorie seed presenti
- test esistenti ancora verdi

**Checklist fase**:

- [x] Package `JwtBearer` e `BCrypt` installati e compilazione OK
- [x] Nuove entita create (`User`, `RefreshToken`, `Prenotazione`, `Categoria`, `FilmCategoria`, `UserRole`)
- [x] `FilmDbContext` aggiornato con DbSet, relazioni e indici unici
- [x] `.env` e `.env.example` aggiornati con variabili JWT/admin
- [x] Config JWT presente in `Program.cs` senza middleware auth attivi
- [x] Migration `AddCategorieAndAuth` creata e applicata
- [x] Seed admin e 12 categorie verificato su DB
- [x] Test regressione baseline verdi

---

### FASE 2 - Categorie e aggiornamento Film per many-to-many

**Obiettivo**: CRUD categorie completo e integrazione categorie nei film.

**Attivita**:

1. Creare DTO categorie:
   - `DTO/CategoriaDTO.cs`
   - `DTO/CategoriaCreateDTO.cs`
   - `DTO/CategoriaUpdateDTO.cs`
2. Aggiornare DTO film:
   - `FilmDTO` -> `List<CategoriaDTO> Categorie`
   - `FilmCreateDTO` -> `List<int>? CategorieIds`
   - `FilmUpdateDTO` -> `List<int>? CategorieIds`
3. Creare `Services/ICategoriaService.cs` + `Services/CategoriaService.cs`.
4. Aggiornare `Services/FilmService.cs`:
   - gestione `CategorieIds` in create/update (sync record ponte)
   - include su `FilmCategorie -> Categoria` in read/list/paged
5. Creare `Endpoints/CategorieEndpoints.cs` con 5 endpoint CRUD.
6. Registrare DI e mapping in `Program.cs`.
7. Definire codici risposta coerenti (201/204/404/409).

**Verifica fase**:

- CRUD categorie funzionante
- film creati/aggiornati con categorie multiple
- output film include categorie

**Checklist fase**:

- [x] DTO categorie creati (`CategoriaDTO`, `CategoriaCreateDTO`, `CategoriaUpdateDTO`)
- [x] DTO film aggiornati con `Categorie` e `CategorieIds`
- [x] `ICategoriaService`/`CategoriaService` implementati con validazione duplicati
- [x] `FilmService` aggiornato per sync `FilmCategoria` in create/update
- [x] Query read film includono `FilmCategorie -> Categoria`
- [x] `CategorieEndpoints` mappati con codici `201/204/404/409`
- [x] Swagger/manual test CRUD categorie completati

---

### FASE 3 - Auth Service e endpoint autenticazione

**Obiettivo**: implementare flusso register/login/refresh/logout/me.

**Attivita**:

1. Creare DTO auth:
   - `LoginRequestDTO`, `RegisterRequestDTO`, `AuthResponseDTO`, `UserInfoDTO`, `RefreshTokenRequestDTO`
2. Creare `Services/IAuthService.cs` + `Services/AuthService.cs`:
   - `RegisterAsync` (BCrypt, ruolo default `User`)
   - `LoginAsync` (verifica credenziali)
   - `RefreshAsync` (validazione + rotazione refresh token)
   - `LogoutAsync` (revoca refresh token)
   - `GetUserByIdAsync`
   - helper `GenerateAccessToken`, `GenerateRefreshToken`
3. Creare `Endpoints/AuthEndpoints.cs`:
   - `POST /auth/register`
   - `POST /auth/login`
   - `POST /auth/refresh`
   - `POST /auth/logout`
   - `GET /auth/me`
4. Registrare e mappare in `Program.cs`.
5. In questa fase, se necessario, gestire manualmente il parsing token su `/auth/me` per test preliminare.

**Verifica fase**:

- register crea utente
- login ritorna coppia token
- refresh rinnova token e revoca il precedente
- credenziali errate -> 401

**Checklist fase**:

- [x] DTO auth creati e validati
- [x] `IAuthService`/`AuthService` implementati con BCrypt
- [x] Generazione JWT con claim `sub/email/role/nome`
- [x] Refresh token persistito con expiry e revoca
- [x] Endpoint `/auth/register|login|refresh|logout|me` mappati
- [x] Casi errore verificati (`401` credenziali, refresh invalid/scaduto)
- [x] Flusso register/login/refresh funzionante end-to-end

---

### FASE 4 - Enforcement RBAC globale su tutte le API

**Obiettivo**: attivare middleware auth e policy su endpoint esistenti e nuovi.

**Attivita**:

1. Attivare middleware in `Program.cs` nell'ordine corretto:

   ```csharp
   app.UseCors("AllowCineBaseFrontend");
   app.UseAuthentication();
   app.UseAuthorization();
   app.UseStaticFiles();
   ```

2. Aggiornare CORS per header `Authorization` (`AllowAnyHeader` o header espliciti).
3. Definire policy:
   - `AdminOnly`
   - `PowerUserOrAdmin`
   - `Authenticated`
4. Applicare `AllowAnonymous`/`RequireAuthorization` a endpoint o gruppi:
   - auth: anonimi su register/login/refresh; autenticati su logout/me
   - registi: `PowerUserOrAdmin`
   - cinemas: GET pubblico, CUD admin
   - films/proiezioni/categorie: GET pubblico, CUD power/admin
   - media upload: power/admin
5. Verificare comportamento uniforme 401 vs 403.

**Verifica fase**:

- senza token su endpoint protetti -> 401
- ruolo non sufficiente -> 403
- permessi allineati alla matrice sezione 2.2

**Checklist fase**:

- [x] `UseAuthentication()` e `UseAuthorization()` abilitati in ordine corretto
- [x] Policy `AdminOnly`, `PowerUserOrAdmin`, `Authenticated` configurate
- [x] CORS consente header `Authorization`
- [x] Endpoint auth con `AllowAnonymous`/`RequireAuthorization` corretti
- [x] Endpoint CRUD mappati alle policy RBAC previste
- [x] Verificati casi 401 (no token) e 403 (ruolo insufficiente)

---

### FASE 5 - Area Personale, Prenotazioni, Gestione Utenti Admin

**Obiettivo**: completare funzionalita utente autenticato e controlli amministrativi.

**Attivita**:

1. Creare DTO:
   - `ProfiloUpdateDTO`
   - `PrenotazioneCreateDTO`
   - `PrenotazioneDTO` (con campi derivati: `TitoloFilm`, `NomeCinema`, `DataProiezione`, `OraProiezione`)
   - `UserAdminDTO`
   - `UpdateRuoloDTO`
2. Creare servizi:
   - `IProfiloService` + `ProfiloService`
   - `IPrenotazioneService` + `PrenotazioneService`
   - `IUserAdminService` + `UserAdminService`
3. Creare endpoint:
   - `GET/PUT /profilo`
   - `GET/POST/DELETE /prenotazioni`
   - `GET /admin/utenti`, `PUT /admin/utenti/{id}/ruolo`
4. Implementare ownership check su prenotazioni.
5. Vincolo sicurezza: impedire degradazione dell'ultimo admin.

**Verifica fase**:

- user vede/modifica solo dati propri
- user gestisce solo prenotazioni proprie
- admin vede tutte le prenotazioni e gestisce ruoli

**Checklist fase**:

- [x] DTO profilo/prenotazioni/admin utenti creati
- [x] Servizi `IProfiloService`, `IPrenotazioneService`, `IUserAdminService` implementati
- [x] Endpoint `/profilo`, `/prenotazioni`, `/admin/utenti` mappati con policy corrette
- [x] Ownership check su prenotazioni implementato e testato
- [x] Vincolo "non degradare ultimo admin" implementato
- [x] `PrenotazioneDTO` include campi derivati (film/cinema/data/ora)

---

### FASE 6 - Aggiornamento e ampliamento test backend

**Obiettivo**: rendere la suite test aderente al nuovo modello di sicurezza.

**Attivita**:

1. Aggiornare `tests/backend/CustomWebApplicationFactory.cs`:
   - `CreateAuthenticatedClient(UserRole role)`
   - `CreateAdminClient()`, `CreatePowerUserClient()`, `CreateUserClient()`, `CreateAnonymousClient()`
   - `ResetDatabaseAsync()` esteso a tabelle nuove
2. Aggiornare integration test esistenti per ruoli corretti.
3. Aggiungere test Auth (`A1`-`A8`): register/login/refresh/logout/me.
4. Aggiungere test RBAC (`RB1`-`RB8`): 401/403 e casi ruolo.
5. Aggiungere test Categorie (`CAT1`-`CAT5`).
6. Aggiungere test Prenotazioni (`PR1`-`PR5`).
7. Eseguire: `dotnet test tests/backend/FilmAPI.Tests.csproj`.

**Verifica fase**:

- vecchi + nuovi test tutti verdi
- copertura minima: auth, RBAC, categorie, prenotazioni

**Checklist fase**:

- [x] `CustomWebApplicationFactory` supporta client autenticati per ruolo
- [x] `ResetDatabaseAsync()` pulisce anche nuove tabelle
- [x] Test esistenti adattati al nuovo modello auth/RBAC
- [x] Suite `A1-A8` aggiunta e verde
- [x] Suite `RB1-RB8` aggiunta e verde
- [x] Suite `CAT1-CAT5` aggiunta e verde
- [x] Suite `PR1-PR5` aggiunta e verde
- [x] `dotnet test tests/backend/FilmAPI.Tests.csproj` completato senza failure

---

### FASE 7 - Frontend Auth reale e token lifecycle

**Obiettivo**: sostituire auth mock con autenticazione reale.

**Attivita**:

1. Creare `wwwroot/js/auth.js`:
   - gestione token (`get/save/clear`)
   - `isLoggedIn`, `getCurrentUser`, `getUserRole`
   - `login`, `register`, `logout`, `refreshAccessToken`
2. Aggiornare `wwwroot/js/api.js`:
   - inject Bearer token automatico
   - retry su 401 con refresh
   - fallback logout + redirect login se refresh fallisce
   - metodi API per profilo/prenotazioni/categorie/utenti
3. Creare `login.html` + `js/pages/login.js`:
   - supporto `?redirect=` e `?expired=true`
4. Creare `registrazione.html` + `js/pages/registrazione.js`:
   - validazione campi e password
5. Aggiornare `components/navbar-landing.html` auth-aware.

**Verifica fase**:

- login/register end-to-end OK
- token salvati/aggiornati correttamente
- chiamate protette funzionano con refresh automatico

**Checklist fase**:

- [x] `auth.js` implementa login/register/logout/refresh + utility token
- [x] `api.js` aggiunge Bearer token automaticamente
- [x] Retry con refresh su `401` implementato
- [x] Fallback sessione scaduta -> clear token + redirect login
- [x] `login.html`/`login.js` supportano `?redirect=` e `?expired=true`
- [x] `registrazione.html`/`registrazione.js` con validazioni client attive
- [x] Navbar landing aggiornata in modalita auth-aware

---

### FASE 8 - Route guard e navigazione per ruolo

**Obiettivo**: bloccare accessi non autorizzati lato pagina e adattare UI.

**Attivita**:

1. Creare `wwwroot/js/route-guard.js`:
   - mappa pagina -> ruoli ammessi
   - redirect per non autenticati, ruoli non ammessi, pagine anonime-only
2. Aggiornare `components/navbar-landing.html`:
   - mostrare voci in base a stato/ruolo
   - nascondere area admin a `User`
3. Aggiornare `components/navbar-admin.html`:
   - link categorie
   - profilo/logout reali
4. Aggiornare `dashboard.html` e `js/navbar.js` rimuovendo logica mock.
5. Includere `auth.js` e `route-guard.js` in tutte le pagine.

**Verifica fase**:

- URL diretto a pagina non consentita -> redirect corretto
- utente `User` non vede bottoni/entry area admin

**Checklist fase**:

- [x] `route-guard.js` creato con mappa permessi pagina->ruolo
- [x] Redirect non autenticato -> login con query `redirect`
- [x] Redirect ruolo insufficiente -> pagina consentita (`index` o `profilo`)
- [x] `navbar-landing` mostra/nasconde voci per stato auth/ruolo
- [x] `navbar-admin` include categorie e logout reale
- [x] `navbar.js` non usa piu mock auth
- [x] `auth.js` e `route-guard.js` inclusi in tutte le pagine

---

### FASE 9 - Programmazione pubblica + gestione categorie admin

**Obiettivo**: separare chiaramente discovery e operativita lato frontend, offrendo:

- una vista pubblica di programmazione dedicata (`programmazione.html`) per ricerca e filtri operativi
- una home (`index.html`) orientata a discovery/marketing con film in evidenza
- strumenti admin per gestione categorie

**Attivita**:

1. Creare `programmazione.html` + `js/pages/programmazione.js`:
   - elenco proiezioni correnti con join film/cinema/categorie
   - filtri per citta, data, categoria
   - bottone prenota auth-aware
2. Creare `categorie.html` + `js/pages/categorie.js` (CRUD).
3. Aggiornare `index.html` e `js/pages/home.js` in ottica **featured discovery**:
   - rimuovere dalla landing i filtri operativi programmazione
   - introdurre sezione "In Evidenza Questa Settimana"
   - layout: hero card grande + mini-grid di card compatte
   - logica featured: priorita ai film con piu proiezioni nei prossimi 7 giorni, fallback su nuove uscite
   - CTA verso `programmazione.html` (prenotazione/search gestita nella pagina dedicata)
4. Aggiornare `films.html` + `js/pages/films.js`:
   - multi-select/checkbox categorie
   - visualizzazione badge categorie
   - filtro per categoria
5. Aggiornare navbar landing con link programmazione.

**Verifica fase**:

- pagina programmazione accessibile ad anonimi in sola lettura
- CRUD categorie disponibile solo a power/admin
- categorie visibili nei film in admin e landing
- landing senza duplicazione della programmazione operativa (filtri solo in `programmazione.html`)
- sezione featured home coerente e deterministica (niente rendering intermittente)

**Checklist fase**:

- [x] `programmazione.html` e `programmazione.js` creati
- [x] Filtri citta/data/categoria funzionanti lato UI
- [x] Pulsante prenota gestisce redirect in base a stato auth
- [x] `categorie.html` e `categorie.js` con CRUD completo
- [x] `films.html`/`films.js` supportano selezione multipla categorie
- [x] `index.html`/`home.js` mostrano badge categorie
- [x] Navbar landing include link programmazione
- [x] Landing riprogettata in modalita featured (hero + mini-grid) con CTA verso `programmazione.html`

---

### FASE 10 - Area Personale utente (profilo + prenotazioni) + ottimizzazione listing admin

**Obiettivo**: completare esperienza utente autenticato fuori dall'area admin e migliorare efficienza/consistenza delle pagine di gestione con volumi dati crescenti.

**Attivita**:

1. Creare `profilo.html` + `js/pages/profilo.js`:
   - sezione dati personali
   - update profilo
   - lista prenotazioni
   - annullo prenotazione
   - creazione nuova prenotazione
2. Gestire parametro `?prenota=<proiezioneId>` proveniente da programmazione.
3. Mostrare feedback operazioni (success/error) in UI.
4. Allineare la UX della sezione home featured (hero + compact cards) su responsive e leggibilita cross-theme.
5. Uniformare stato/disponibilita nelle tabelle admin (`dashboard`/`proiezioni`) e migliorare leggibilita relazioni film/cinema.
6. Introdurre paginazione e ricerca su `registi.html`, `cinemas.html`, `proiezioni.html` con UI coerente a `films.html`.
7. Estendere API backend per paginazione/search server-side su `GET /registi`, `GET /cinemas`, `GET /proiezioni` mantenendo compatibilita legacy senza query params.
8. Aggiornare test di integrazione backend per coprire paginazione e compatibilita legacy sui tre endpoint.

**Verifica fase**:

- flusso programmazione -> login -> profilo -> prenotazione completo
- utente modifica i propri dati e gestisce prenotazioni proprie
- listing admin navigabili in pagine e filtrabili senza caricare dataset completi
- endpoint `registi/cinemas/proiezioni` supportano payload paginato (`items/page/pageSize/totalCount/totalPages`) e continuano a restituire array legacy se chiamati senza parametri
- suite backend completamente verde dopo ampliamento test

**Checklist fase**:

- [x] `profilo.html` e `profilo.js` creati e protetti da route guard
- [x] Sezione dati personali con update profilo funzionante
- [x] Lista prenotazioni utente caricata e aggiornata
- [x] Cancellazione prenotazione propria funzionante
- [x] Creazione prenotazione da form e da query `?prenota=` funzionante
- [x] Messaggi di feedback UI (success/error) presenti
- [x] Home featured rifinita (layout responsive, contrasto overlay, proporzioni hero/mini-card)
- [x] Stato proiezioni uniformato tra dashboard/proiezioni con chip arrotondate coerenti
- [x] Colonne film/cinema in proiezioni arricchite con nome/titolo + ID visualmente de-enfatizzato
- [x] Ricerca aggiunta su `cinemas.html` e `proiezioni.html`
- [x] Paginazione UI aggiunta su `registi.html`, `cinemas.html`, `proiezioni.html`
- [x] Backend: `GET /registi|/cinemas|/proiezioni` con supporto `page/pageSize/search` + DTO paginati dedicati
- [x] Compatibilita legacy endpoint list preservata (senza query params -> array)
- [x] Test integrazione backend estesi per paginazione e compatibilita legacy (`R10-R11`, `C6-C7`, `P9-P10`)
- [x] `dotnet test tests/backend/FilmAPI.Tests.csproj` verde (**103/103 PASS**)

---

### FASE 11 - Verifica finale, hardening e documentazione

**Obiettivo**: chiudere iterazione con qualita verificata e documentazione aggiornata.

**Attivita**:

1. Eseguire test backend completi.
2. Eseguire checklist manuale per ruoli:
   - Admin: CRUD completo + gestione utenti/ruoli
   - PowerUser: CRUD film/registi/proiezioni/categorie + cinema read-only
   - User: programmazione + profilo + prenotazioni; no area admin
   - Anonimo: index/programmazione sola lettura; prenota -> login
3. Validare redirect URL diretti non autorizzati.
4. Aggiornare `docs/project/status.md`.
5. Aggiornare `docs/project/changelog.md`.

**Verifica fase**:

- test verdi
- RBAC e redirect coerenti
- documentazione allineata allo stato finale

**Checklist fase**:

- [x] Eseguita suite test backend completa e salvato esito (103/103 PASS)
- [x] Verifica manuale completata per Admin/PowerUser/User/Anonimo
- [x] Verifica redirect su URL diretti non autorizzati completata
- [x] Aggiornato `docs/project/status.md`
- [x] Aggiornato `docs/project/changelog.md`
- [x] Piano finale allineato a eventuali scostamenti implementativi

---

## 5) Nuovi File Previsti

## 5.1 Backend (`backend/FilmAPI/`)

- `Model/UserRole.cs`
- `Model/User.cs`
- `Model/RefreshToken.cs`
- `Model/Prenotazione.cs`
- `Model/Categoria.cs`
- `Model/FilmCategoria.cs`
- `DTO/CategoriaDTO.cs`
- `DTO/CategoriaCreateDTO.cs`
- `DTO/CategoriaUpdateDTO.cs`
- `DTO/LoginRequestDTO.cs`
- `DTO/RegisterRequestDTO.cs`
- `DTO/AuthResponseDTO.cs`
- `DTO/UserInfoDTO.cs`
- `DTO/RefreshTokenRequestDTO.cs`
- `DTO/ProfiloUpdateDTO.cs`
- `DTO/PrenotazioneDTO.cs`
- `DTO/PrenotazioneCreateDTO.cs`
- `DTO/UserAdminDTO.cs`
- `DTO/UpdateRuoloDTO.cs`
- `Services/IAuthService.cs`
- `Services/AuthService.cs`
- `Services/ICategoriaService.cs`
- `Services/CategoriaService.cs`
- `Services/IProfiloService.cs`
- `Services/ProfiloService.cs`
- `Services/IPrenotazioneService.cs`
- `Services/PrenotazioneService.cs`
- `Services/IUserAdminService.cs`
- `Services/UserAdminService.cs`
- `Endpoints/AuthEndpoints.cs`
- `Endpoints/CategorieEndpoints.cs`
- `Endpoints/ProfiloEndpoints.cs`
- `Endpoints/PrenotazioniEndpoints.cs`
- `Endpoints/AdminUtentiEndpoints.cs`

## 5.2 Frontend (`frontend/CineBase.Web/wwwroot/`)

- `login.html`
- `registrazione.html`
- `programmazione.html`
- `profilo.html`
- `categorie.html`
- `js/auth.js`
- `js/route-guard.js`
- `js/pages/login.js`
- `js/pages/registrazione.js`
- `js/pages/programmazione.js`
- `js/pages/profilo.js`
- `js/pages/categorie.js`

---

## 6) Criteri di Accettazione

L'iterazione e completata quando tutte le seguenti condizioni sono vere:

1. anonimo accede a `index.html` e `programmazione.html` ma non puo prenotare
2. registrazione/login producono token JWT validi
3. refresh token rinnova access token senza nuovo login
4. `User` gestisce profilo e prenotazioni proprie
5. `User` non accede alle pagine admin e non vede bottone area admin
6. `PowerUser` fa CRUD su Film/Proiezioni/Registi/Categorie e solo Read su Cinema
7. `Admin` fa tutto e gestisce ruoli utenti
8. categorie associate ai film, visualizzate e filtrabili
9. API rispondono con 401/403 coerenti
10. redirect frontend coerenti per tutti i casi non autorizzati
11. suite backend (`tests/backend/FilmAPI.Tests.csproj`) totalmente verde

---

## 7) Prompt Guida (per esecuzione fase-by-fase)

Regola comune per **tutti** i prompt fase:

- implementare solo la fase richiesta
- al termine aggiornare la tabella `Stato Avanzamento Fasi`:
  - `Stato`: `Completata` (oppure `In corso` / `Bloccata`)
  - `Data`: data corrente
  - `Note`: breve esito (test, blocchi, deviazioni)
- spuntare la `Checklist fase` relativa con `[x]` sugli item completati
- se restano attivita parziali, lasciare check non spuntati e indicare motivo nelle note

**Fase 1**
"Implementa Fase 1 del piano: crea modelli UserRole, User, RefreshToken, Prenotazione, Categoria, FilmCategoria; aggiorna Film e FilmDbContext; installa JwtBearer e BCrypt; configura JWT in Program.cs senza abilitare middleware; aggiorna env; crea migration AddCategorieAndAuth; implementa seed admin + 12 categorie; verifica test esistenti verdi. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 1."

**Fase 2**
"Implementa Fase 2 del piano: crea ICategoriaService/CategoriaService CRUD, DTO categorie, aggiorna DTO e FilmService per CategorieIds e include, crea endpoint categorie con codici risposta coerenti, registra DI e mapping, testa via Swagger. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 2."

**Fase 3**
"Implementa Fase 3 del piano: crea IAuthService/AuthService con register/login/refresh/logout/me, DTO auth, AuthEndpoints, registra DI e mapping, verifica flussi e codici errore. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 3."

**Fase 4**
"Implementa Fase 4 del piano: attiva UseAuthentication/UseAuthorization, configura policy AdminOnly/PowerUserOrAdmin/Authenticated, aggiorna CORS per Authorization header, applica AllowAnonymous/RequireAuthorization a tutti gli endpoint secondo matrice permessi. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 4."

**Fase 5**
"Implementa Fase 5 del piano: crea servizi profilo, prenotazioni, admin utenti; endpoint /profilo, /prenotazioni, /admin/utenti; ownership check; impedisci downgrade ultimo admin; testa via Swagger. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 5."

**Fase 6**
"Implementa Fase 6 del piano: aggiorna CustomWebApplicationFactory con helper client autenticati e reset DB esteso, aggiorna test esistenti, aggiungi A1-A8, RB1-RB8, CAT1-CAT5, PR1-PR5, esegui dotnet test e verifica tutto verde. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 6."

**Fase 7**
"Implementa Fase 7 del piano: crea auth.js, aggiorna api.js con Bearer + refresh interceptor, crea login/registrazione con JS, aggiorna navbar landing auth-aware. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 7."

**Fase 8**
"Implementa Fase 8 del piano: crea route-guard.js con mappa permessi pagine e redirect, aggiorna navbar landing/admin, dashboard e navbar.js rimuovendo mock auth, includi auth.js/route-guard.js in tutte le pagine. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 8."

**Fase 9**
"Implementa Fase 9 del piano: crea programmazione.html con filtri citta/data/categoria e prenota auth-aware, crea categorie.html CRUD admin, aggiorna films/index/home per categorie (badge + multi-select + filtro). A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 9."

**Fase 10**
"Implementa Fase 10 del piano: crea profilo.html con dati personali e prenotazioni, supporta parametro ?prenota=id, implementa modifica profilo e annullamento prenotazioni. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 10."

**Fase 11**
"Implementa Fase 11 del piano: esegui verifica finale con test verdi, verifica manuale flussi Admin/PowerUser/User/Anonimo, verifica redirect, aggiorna status.md e changelog.md. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 11."
