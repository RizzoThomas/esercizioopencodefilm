# Piano di Lavoro - Iterazione 3: Sicurezza API, Autenticazione JWT, RBAC, Categorie Film e Area Personale Utente

Autore: OpenCode con MiMo V2 Pro High

## 1) Obiettivo

Trasformare CineBase da applicazione "tutto aperto" a sistema sicuro con autenticazione JWT, controllo accessi basato su ruoli (RBAC), gestione categorie film (many-to-many), area personale utente con prenotazioni virtuali e protezione route frontend.

### 1.1 Stato attuale

| Componente | Stato |
| --- | --- |
| Backend API | ASP.NET 9 Minimal API, 4 entità (Film, Regista, Cinema, Proiezione), 5 servizi CRUD |
| Frontend | MPA statico (porta 5001), 6 pagine HTML, autenticazione mock in sessionStorage |
| Test | 71 test (32 unit + 39 integration), tutti verdi |
| Sicurezza | Nessuna. API completamente aperte, nessun token, nessun ruolo |

### 1.2 Architettura repo

```text
repo-root/
├── backend/FilmAPI/          ← API .NET 9 (porta 5000)
├── frontend/CineBase.Web/    ← Static files (porta 5001)
├── tests/backend/            ← xUnit + FluentAssertions
└── docs/
```

---

## 2) Ruoli e Permessi

### 2.1 Definizione ruoli

| Ruolo | Enum | Descrizione |
| --- |--- | --- |
| **Admin** | `2` | Accesso totale. CRUD su tutto, gestione utenti e ruoli |
| **PowerUser** | `1` | CRUD su Film, Proiezioni, Registi, Categorie. Solo Read su Cinema |
| **User** | `0` | Autenticato. Consulta programmazione, prenota, gestisce profilo. Niente area admin |
| **Anonimo** | - | Non autenticato. Solo index.html e programmazione.html in lettura |

### 2.2 Matrice permessi API

| Gruppo Endpoint | GET | POST/PUT/DELETE | Policy |
| --- | --- | --- | --- |
| `/auth/register`, `/auth/login`, `/auth/refresh` | - | Anonimo | `[AllowAnonymous]` |
| `/auth/logout`, `/auth/me` | Autenticato | Autenticato | `[Authorize]` |
| `/categorie` | Tutti | PowerUser, Admin | GET: `[AllowAnonymous]`, CUD: `PowerUserOrAdmin` |
| `/films` | Tutti | PowerUser, Admin | GET: `[AllowAnonymous]`, CUD: `PowerUserOrAdmin` |
| `/registi` | PowerUser, Admin | PowerUser, Admin | Tutto: `PowerUserOrAdmin` |
| `/cinemas` | Tutti | Solo Admin | GET: `[AllowAnonymous]`, CUD: `AdminOnly` |
| `/proiezioni` | Tutti | PowerUser, Admin | GET: `[AllowAnonymous]`, CUD: `PowerUserOrAdmin` |
| `/media/covers` | - | PowerUser, Admin | `PowerUserOrAdmin` |
| `/profilo` | Autenticato | Autenticato | `[Authorize]` |
| `/prenotazioni` | User (proprie), Admin (tutte) | User (create/delete proprie), Admin | `[Authorize]` + ownership check |
| `/admin/utenti` | Solo Admin | Solo Admin | `AdminOnly` |

### 2.3 Matrice permessi pagine frontend

| Pagina | Anonimo | User | PowerUser | Admin |
| --- | --- | --- | --- | --- |
| `index.html` | SI | SI | SI | SI |
| `programmazione.html` | SI | SI | SI | SI |
| `login.html` | SI | - | - | - |
| `registrazione.html` | SI | - | - | - |
| `profilo.html` | - | SI | SI | SI |
| `dashboard.html` | - | - | SI | SI |
| `films.html` | - | - | SI | SI |
| `registi.html` | - | - | SI | SI |
| `cinemas.html` | - | - | SI | SI |
| `proiezioni.html` | - | - | SI | SI |
| `categorie.html` | - | - | SI | SI |

---

## 3) Design Tecnico

### 3.1 Nuove entità

**Categoria** — tabella lookup per genere cinematografico

```text
Categoria(Id int PK, Nome string required unique max 100)
Navigation: ICollection<FilmCategoria>
```

**FilmCategoria** — tabella ponte M-N Film↔Categoria

```text
FilmCategoria(FilmId int FK, CategoriaId int FK) -- PK composita
Navigation: Film, Categoria
```

**User** — utente dell'applicazione

```text
User(Id int PK, Email string required unique, PasswordHash string required,
     Nome string required max 100, Cognome string required max 100,
     Telefono string? max 20, Ruolo enum UserRole required,
     DataRegistrazione DateTime required)
Navigation: ICollection<RefreshToken>, ICollection<Prenotazione>
```

**RefreshToken** — token di refresh JWT

```text
RefreshToken(Id int PK, Token string required unique,
             UserId int FK, ExpiresAt DateTime required,
             CreatedAt DateTime required, RevokedAt DateTime?)
Computed: IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow
```

**Prenotazione** — prenotazione virtuale senza pagamento

```text
Prenotazione(Id int PK, UserId int FK, ProiezioneId int FK,
             NumeroPosti int required, Note string? max 500,
             DataPrenotazione DateTime required)
```

### 3.2 Modifiche entità esistenti

**Film** — aggiungere navigation per categorie

```text
+ ICollection<FilmCategoria> FilmCategorie
```

### 3.3 Relazioni aggiornate

```text
Regista    1 ---< N  Film           (esistente, Restrict)
Cinema     1 ---< N  Proiezione     (esistente, Restrict)
Film       1 ---< N  Proiezione     (esistente, Restrict)
Film       M >---< N  Categoria      (tramite FilmCategoria, cascade)
User       1 ---< N  RefreshToken   (cascade delete)
User       1 ---< N  Prenotazione   (cascade delete)
Proiezione 1 ---< N  Prenotazione   (Restrict)
```

### 3.4 JWT Design

| Parametro | Access Token | Refresh Token |
| --- | --- | --- |
| Formato | JWT (HS256) | GUID random stringa |
| Durata | 15 minuti | 7 giorni |
| Claims | `sub` (userId), `email`, `role` (UserRole enum value), `nome` | Nessun claim, solo token opaco |
| Storage frontend | `localStorage` | `localStorage` |
| Rinnovo | Via refresh endpoint | Rotazione: revoca il vecchio, emette nuovo |

### 3.5 Pacchetti NuGet da aggiungere

```text
Microsoft.AspNetCore.Authentication.JwtBearer 9.x
BCrypt.Net-Next
```

### 3.6 Variabili .env da aggiungere

```env
JWT_SECRET=<chiave segreta minimo 256 bit>
JWT_ISSUER=CineBaseAPI
JWT_AUDIENCE=CineBaseWeb
JWT_ACCESS_TOKEN_EXPIRY_MINUTES=15
JWT_REFRESH_TOKEN_EXPIRY_DAYS=7
ADMIN_SEED_EMAIL=admin@cinebase.it
ADMIN_SEED_PASSWORD=Admin123!
```

### 3.7 Categorie seed iniziali

```text
Drammatico, Commedia, Avventura, Fantasy, Horror, Azione,
Fantascienza, Thriller, Animazione, Documentario, Romantico, Storico
```

---

## 4) Fasi di Implementazione

---

### FASE 1 — Modello Dati, Infrastruttura JWT e Seed

**Obiettivo**: Creare tutte le nuove entità, aggiornare DbContext, configurare JWT in Program.cs (senza attivare ancora il middleware), creare migration e seed admin + categorie.

**Attività**:

1. Installare pacchetti NuGet in `backend/FilmAPI/`:
   - `dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer`
   - `dotnet add package BCrypt.Net-Next`

2. Creare `Model/UserRole.cs` — enum con valori: `User = 0, PowerUser = 1, Admin = 2`

3. Creare `Model/User.cs` con proprietà: Id, Email, PasswordHash, Nome, Cognome, Telefono?, Ruolo (UserRole), DataRegistrazione. Navigation: RefreshTokens, Prenotazioni

4. Creare `Model/RefreshToken.cs` con proprietà: Id, Token, UserId, ExpiresAt, CreatedAt, RevokedAt?. Proprietà computed `IsActive`

5. Creare `Model/Prenotazione.cs` con proprietà: Id, UserId, ProiezioneId, NumeroPosti, Note?, DataPrenotazione

6. Creare `Model/Categoria.cs` con proprietà: Id, Nome. Navigation: FilmCategorie

7. Creare `Model/FilmCategoria.cs` con proprietà: FilmId (FK), CategoriaId (FK). PK composita. Navigation: Film, Categoria

8. Aggiornare `Model/Film.cs` — aggiungere `ICollection<FilmCategoria> FilmCategorie`

9. Aggiornare `Data/FilmDbContext.cs`:
   - Nuovi DbSet: `Users`, `RefreshTokens`, `Prenotazioni`, `Categorie`, `FilmCategorie`
   - Fluent API: PK composita su `FilmCategoria(FilmId, CategoriaId)`
   - Unique index su `Categoria.Nome`, `User.Email`, `RefreshToken.Token`
   - Relazione Film↔Categoria M-N via FilmCategoria (cascade)
   - Relazione User↔RefreshToken (cascade)
   - Relazione User↔Prenotazione (cascade)
   - Relazione Proiezione↔Prenotazione (Restrict)

10. Aggiornare `.env` e `.env.example` con variabili JWT e seed admin

11. Configurare JWT in `Program.cs`:
    - `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {...})`
    - Leggere `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE` da env
    - **NON** attivare ancora `app.UseAuthentication()` / `app.UseAuthorization()` (fase 4)

12. Creare migration: `dotnet ef migrations add AddCategorieAndAuth`

13. Implementare seed in avvio app (dopo `EnsureCreated`/`Migrate`):
    - Se tabella Users vuota → creare utente admin con email/password da env, ruolo Admin
    - Se tabella Categorie vuota → inserire 12 categorie seed

14. Applicare migration e verificare seed

**Verifica**: Admin presente nel DB, 12 categorie presenti, test esistenti ancora tutti verdi

---

### FASE 2 — Servizio e Endpoint Categorie

**Obiettivo**: CRUD completo per categorie + aggiornamento FilmService per gestire categorie multiple nei film.

**Attività**:

1. Creare DTO:
   - `DTO/CategoriaDTO.cs` — read: Id, Nome
   - `DTO/CategoriaCreateDTO.cs` — input create: Nome
   - `DTO/CategoriaUpdateDTO.cs` — input update: Nome

2. Aggiornare DTO Film per categorie:
   - `FilmDTO`: aggiungere `List<CategoriaDTO> Categorie`
   - `FilmCreateDTO`: aggiungere `List<int>? CategorieIds`
   - `FilmUpdateDTO`: aggiungere `List<int>? CategorieIds`

3. Creare `Services/ICategoriaService.cs` + `Services/CategoriaService.cs`:
   - `GetAllAsync()` → `Task<List<CategoriaDTO>>`
   - `GetByIdAsync(int id)` → `Task<CategoriaDTO?>`
   - `CreateAsync(CategoriaCreateDTO dto)` → `Task<CategoriaDTO>`
   - `UpdateAsync(int id, CategoriaUpdateDTO dto)` → `Task<CategoriaDTO?>`
   - `DeleteAsync(int id)` → `Task<bool>`
   - Validare unicità Nome su create/update

4. Aggiornare `Services/FilmService.cs`:
   - `CreateAsync`: se `CategorieIds` presente, creare record `FilmCategoria`
   - `UpdateAsync`: sincronizzare record `FilmCategoria` (rimuovere vecchie, aggiungere nuove)
   - `GetAllAsync`/`GetPagedAsync`/`GetByIdAsync`: `.Include(f => f.FilmCategorie).ThenInclude(fc => fc.Categoria)` per popolare `Categorie` nel DTO

5. Creare `Endpoints/CategorieEndpoints.cs`:
   - `MapCategorieEndpoints(this WebApplication app)`
   - `GET /categorie` — lista
   - `GET /categorie/{id}` — dettaglio
   - `POST /categorie` — create (ritorna 201, 409 se duplicato)
   - `PUT /categorie/{id}` — update (ritorna 200, 404, 409)
   - `DELETE /categorie/{id}` — delete (ritorna 204, 404)

6. Registrare `ICategoriaService` → `CategoriaService` (Scoped) in `Program.cs`

7. Mappare `app.MapCategorieEndpoints()` in `Program.cs`

**Verifica**: Swagger CRUD categorie OK, film con categorie create/update/read funzionanti

---

### FASE 3 — Servizio Autenticazione e Endpoint Auth

**Obiettivo**: Implementare register, login, refresh token, logout, whoami.

**Attività**:

1. Creare DTO auth:
   - `DTO/LoginRequestDTO.cs` — Email, Password
   - `DTO/RegisterRequestDTO.cs` — Email, Password, Nome, Cognome, Telefono?
   - `DTO/AuthResponseDTO.cs` — AccessToken, RefreshToken, ExpiresAt, UserInfo
   - `DTO/UserInfoDTO.cs` — Id, Email, Nome, Cognome, Telefono, Ruolo
   - `DTO/RefreshTokenRequestDTO.cs` — RefreshToken

2. Creare `Services/IAuthService.cs` + `Services/AuthService.cs`:
   - `RegisterAsync(RegisterRequestDTO)` → `Task<AuthResponseDTO>` — hash password con BCrypt, crea User (ruolo default User), genera token
   - `LoginAsync(LoginRequestDTO)` → `Task<AuthResponseDTO>` — verifica email/password BCrypt, genera access + refresh token
   - `RefreshAsync(RefreshTokenRequestDTO)` → `Task<AuthResponseDTO>` — valida refresh token, revoca il vecchio, emette nuova coppia
   - `LogoutAsync(string refreshToken)` → `Task<bool>` — revoca refresh token
   - `GetUserByIdAsync(int userId)` → `Task<UserInfoDTO?>`
   - Helper privati:
     - `GenerateAccessToken(User)` → genera JWT con claims sub, email, role, nome
     - `GenerateRefreshToken()` → genera GUID random, salva in DB con expiry

3. Creare `Endpoints/AuthEndpoints.cs`:
   - `MapAuthEndpoints(this WebApplication app)`
   - `POST /auth/register` — register (409 se email esiste, 400 se dati invalidi)
   - `POST /auth/login` — login (401 se credenziali errate)
   - `POST /auth/refresh` — refresh (401 se refresh token non valido/scaduto/revocato)
   - `POST /auth/logout` — logout (revoca refresh token dal body)
   - `GET /auth/me` — ritorna UserInfoDTO dell'utente corrente (legge claim sub dal token)

4. Registrare `IAuthService` → `AuthService` in `Program.cs`

5. Mappare `app.MapAuthEndpoints()` in `Program.cs`

**Nota**: In questa fase il middleware auth non è ancora attivo. Gli endpoint auth gestiscono manualmente la validazione del token solo per `/auth/me`.

**Verifica**: Register crea utente, login ritorna token, refresh rinnova, credenziali errate → 401

---

### FASE 4 — Middleware Autorizzazione e RBAC su tutti gli Endpoint

**Obiettivo**: Attivare JWT middleware e applicare policy di autorizzazione a tutti gli endpoint esistenti.

**Attività**:

1. Attivare middleware in `Program.cs` (ordine critico):

   ```csharp
   app.UseCors("AllowCineBaseFrontend");
   app.UseAuthentication();    // ← NUOVO
   app.UseAuthorization();     // ← NUOVO
   app.UseStaticFiles();
   ```

2. Aggiornare CORS policy per supportare header `Authorization`:
   - `.WithHeaders("Content-Type", "Authorization")` oppure `.AllowAnyHeader()`

3. Definire policy di autorizzazione in `Program.cs`:

   ```csharp
   builder.Services.AddAuthorization(options => {
       options.AddPolicy("AdminOnly", policy =>
           policy.RequireRole("Admin"));
       options.AddPolicy("PowerUserOrAdmin", policy =>
           policy.RequireRole("Admin", "PowerUser"));
   });
   ```

4. Applicare `[AllowAnonymous]` e `[Authorize]` su tutti gli endpoint:

   | File endpoint | Modifiche |
   |---|---|
   | `AuthEndpoints.cs` | register/login/refresh: `.AllowAnonymous()`. logout/me: `.RequireAuthorization()` |
   | `CategorieEndpoints.cs` | GET: `.AllowAnonymous()`. POST/PUT/DELETE: `.RequireAuthorization("PowerUserOrAdmin")` |
   | `FilmsEndpoints.cs` | GET: `.AllowAnonymous()`. POST/PUT/DELETE: `.RequireAuthorization("PowerUserOrAdmin")` |
   | `RegistiEndpoints.cs` | Tutti: `.RequireAuthorization("PowerUserOrAdmin")` |
   | `CinemasEndpoints.cs` | GET: `.AllowAnonymous()`. POST/PUT/DELETE: `.RequireAuthorization("AdminOnly")` |
   | `ProiezioniEndpoints.cs` | GET: `.AllowAnonymous()`. POST/PUT/DELETE: `.RequireAuthorization("PowerUserOrAdmin")` |
   | `MediaEndpoints.cs` | POST: `.RequireAuthorization("PowerUserOrAdmin")` |

5. Per endpoint che usano `MapGroup`, applicare `.RequireAuthorization()` al gruppo e sovrascrivere con `.AllowAnonymous()` sui singoli GET dove necessario.

6. Verificare che Swagger in Development continui a funzionare e possa testare endpoint protetti.

**Verifica**: Chiamate senza token ricevono 401. User normale su POST /cinemas riceve 403. Admin e PowerUser accedono ai propri endpoint.

---

### FASE 5 — Area Personale, Prenotazioni e Gestione Utenti Admin

**Obiettivo**: Endpoint per profilo utente, prenotazioni virtuali e gestione utenti lato admin.

**Attività**:

1. Creare DTO:
   - `DTO/ProfiloUpdateDTO.cs` — Nome, Cognome, Telefono?
   - `DTO/PrenotazioneDTO.cs` — Id, ProiezioneId, UserId, NumeroPosti, Note?, DataPrenotazione, + campi derivati (TitoloFilm, NomeCinema, DataProiezione, OraProiezione)
   - `DTO/PrenotazioneCreateDTO.cs` — ProiezioneId, NumeroPosti, Note?
   - `DTO/UserAdminDTO.cs` — Id, Email, Nome, Cognome, Ruolo, DataRegistrazione
   - `DTO/UpdateRuoloDTO.cs` — Ruolo (UserRole)

2. Creare `Services/IProfiloService.cs` + `Services/ProfiloService.cs`:
   - `GetProfiloAsync(int userId)` → `Task<UserInfoDTO?>`
   - `UpdateProfiloAsync(int userId, ProfiloUpdateDTO)` → `Task<UserInfoDTO?>`
   - Validare che userId corrisponda all'utente loggato

3. Creare `Services/IPrenotazioneService.cs` + `Services/PrenotazioneService.cs`:
   - `GetByUserIdAsync(int userId)` → `Task<List<PrenotazioneDTO>>`
   - `GetAllAsync()` → `Task<List<PrenotazioneDTO>>` (solo admin)
   - `CreateAsync(int userId, PrenotazioneCreateDTO)` → `Task<PrenotazioneDTO>`
   - `DeleteAsync(int userId, int prenotazioneId, bool isAdmin)` → `Task<bool>`
   - Include Film, Cinema, Proiezione nei DTO di risposta

4. Creare `Services/IUserAdminService.cs` + `Services/UserAdminService.cs`:
   - `GetAllUsersAsync()` → `Task<List<UserAdminDTO>>`
   - `UpdateUserRoleAsync(int userId, UpdateRuoloDTO)` → `Task<bool>`
   - Impedire di degradare l'ultimo admin

5. Creare endpoint:
   - `Endpoints/ProfiloEndpoints.cs`: `GET /profilo`, `PUT /profilo` — `[Authorize]`
   - `Endpoints/PrenotazioniEndpoints.cs`:
     - `GET /prenotazioni` — user vede proprie, admin vede tutte
     - `POST /prenotazioni` — solo user autenticato
     - `DELETE /prenotazioni/{id}` — user proprie, admin qualsiasi
   - `Endpoints/AdminUtentiEndpoints.cs`:
     - `GET /admin/utenti` — lista utenti, `AdminOnly`
     - `PUT /admin/utenti/{id}/ruolo` — cambio ruolo, `AdminOnly`

6. Registrare servizi in `Program.cs` e mappare endpoint

**Verifica**: User crea/gestisce solo proprie prenotazioni. Admin legge tutte le prenotazioni e gestisce ruoli.

---

### FASE 6 — Aggiornamento e Ampliamento Test Backend

**Obiettivo**: Aggiornare test esistenti per funzionare con auth, aggiungere nuovi test per auth, RBAC, categorie e prenotazioni.

**Attività**:

1. Aggiornare `tests/backend/CustomWebApplicationFactory.cs`:
   - Aggiungere helper per creare client autenticati:
     - `CreateAuthenticatedClient(UserRole role)` — crea utente test nel DB, fa login, restituisce HttpClient con Bearer token
     - `CreateAdminClient()`, `CreatePowerUserClient()`, `CreateUserClient()`, `CreateAnonymousClient()`
   - Assicurarsi che `ResetDatabaseAsync()` pulisca anche tabelle User/RefreshToken/Prenotazioni/Categorie

2. Aggiornare test integration esistenti (R1-R9, F1-F10, C1-C5, P1-P8, M1-M3):
   - Usare client appropriato per ogni test (admin per CUD, anonimo per GET pubblici)
   - Rinominare seguendo convenzione coerente

3. Aggiungere test autenticazione (prefisso `A`):
   - `A1_Register_ReturnsCreated` — registra utente, ritorna token
   - `A2_Register_ReturnsConflict_EmailExists` — email duplicata → 409
   - `A3_Login_ReturnsOk_ValidCredentials` — login corretto → 200 + token
   - `A4_Login_ReturnsUnauthorized_InvalidCredentials` — password errata → 401
   - `A5_Refresh_ReturnsOk_ValidRefreshToken` — refresh valido → nuovo token
   - `A6_Refresh_ReturnsUnauthorized_ExpiredToken` — refresh scaduto → 401
   - `A7_Logout_RevokesRefreshToken` — logout revoca token
   - `A8_Me_ReturnsUserInfo_WhenAuthenticated` — /auth/me ritorna profilo

4. Aggiungere test RBAC (prefisso `RB`):
   - `RB1_PostFilms_ReturnsForbidden_UserRole` — user non può creare film → 403
   - `RB2_PostCinemas_ReturnsForbidden_PowerUser` — power user non può creare cinema → 403
   - `RB3_PostCinemas_ReturnsCreated_Admin` — admin crea cinema → 201
   - `RB4_GetFilms_ReturnsOk_Anonymous` — anonimo legge film → 200
   - `RB5_GetRegisti_ReturnsForbidden_Anonymous` — anonimo non legge registi → 401
   - `RB6_PostFilms_ReturnsCreated_PowerUser` — power user crea film → 201
   - `RB7_DeletePrenotazione_ReturnsForbidden_OtherUser` — user non cancella prenotazione altrui → 403
   - `RB8_PutCategorie_ReturnsForbidden_User` — user non modifica categorie → 403

5. Aggiungere test categorie (prefisso `CAT`):
   - `CAT1_PostCategorie_ReturnsCreated` — crea categoria
   - `CAT2_GetCategorie_ReturnsAll` — lista categorie
   - `CAT3_PutCategorie_ReturnsOk` — aggiorna categoria
   - `CAT4_DeleteCategorie_ReturnsNoContent` — elimina categoria
   - `CAT5_PostFilms_WithCategories_ReturnsCreated` — film con categorie

6. Aggiungere test prenotazioni (prefisso `PR`):
   - `PR1_PostPrenotazioni_ReturnsCreated` — crea prenotazione
   - `PR2_GetPrenotazioni_ReturnsUserBookings` — user vede proprie
   - `PR3_DeletePrenotazione_ReturnsNoContent` — elimina propria
   - `PR4_GetPrenotazioni_ReturnsAll_Admin` — admin vede tutte
   - `PR5_PostPrenotazioni_ReturnsBadRequest_InvalidProiezione` — proiezione inesistente

7. Eseguire: `dotnet test tests/backend/FilmAPI.Tests.csproj`

**Verifica**: Tutti i test verdi (vecchi + nuovi). Copertura auth, RBAC, categorie, prenotazioni.

---

### FASE 7 — Frontend: Autenticazione e Gestione Token

**Obiettivo**: Login/registrazione reali, token management lato client, API client con Bearer token e refresh automatico.

**Attività**:

1. Creare `wwwroot/js/auth.js` — modulo autenticazione:
   - `getAccessToken()` / `getRefreshToken()` — leggono da localStorage
   - `saveTokens(accessToken, refreshToken)` — salvano in localStorage
   - `clearTokens()` — rimuovono da localStorage
   - `isLoggedIn()` — true se access token presente
   - `getCurrentUser()` — decodifica JWT (base64 payload) per ottenere claims
   - `getUserRole()` — ritorna 'Admin' | 'PowerUser' | 'User' | null
   - `login(email, password)` — chiama `POST /auth/login`, salva token
   - `register(data)` — chiama `POST /auth/register`, salva token
   - `logout()` — chiama `POST /auth/logout`, pulisce token, redirect index
   - `refreshAccessToken()` — chiama `POST /auth/refresh`, aggiorna token

2. Aggiornare `wwwroot/js/api.js`:
   - Aggiungere header `Authorization: Bearer <token>` automatico in `apiFetch()`
   - Se risposta 401 (non su endpoint auth), tentare refresh → retry
   - Se refresh fallisce: clearTokens + redirect `/login.html?expired=true`
   - Aggiungere metodi nuovi:
     - `API.login(data)`, `API.register(data)`, `API.logout(refreshToken)`
     - `API.getProfilo()`, `API.updateProfilo(data)`
     - `API.getPrenotazioni()`, `API.createPrenotazione(data)`, `API.deletePrenotazione(id)`
     - `API.getCategorie()`, `API.createCategoria(data)`, `API.updateCategoria(id, data)`, `API.deleteCategoria(id)`
     - `API.getUtenti()`, `API.updateRuolo(userId, data)`

3. Creare `wwwroot/login.html`:
   - Tema scuro (landing style)
   - Form: Email, Password, submit
   - Link a registrazione
   - Query param `?redirect=` per redirect post-login
   - Query param `?expired=true` per messaggio sessione scaduta

4. Creare `wwwroot/js/pages/login.js`:
   - Gestione submit form login
   - Chiamata `API.login()`
   - Redirect post-login a `?redirect=` o index
   - Gestione errori (401 → messaggio credenziali errate)

5. Creare `wwwroot/registrazione.html`:
   - Tema scuro (landing style)
   - Form: Nome, Cognome, Email, Password, Conferma Password, Telefono (opzionale)
   - Link a login

6. Creare `wwwroot/js/pages/registrazione.js`:
   - Validazione client (password match, campi required)
   - Chiamata `API.register()`
   - Redirect post-register a index

7. Aggiornare `wwwroot/components/navbar-landing.html`:
   - Se non loggato: link "Accedi" → `/login.html`
   - Se loggato: nome utente + dropdown (Profilo, Logout)
   - Nascondere "Area Admin" per ruolo User

**Verifica**: Login/register funzionanti end-to-end, token salvati, chiamate API con Bearer header, refresh automatico su 401

---

### FASE 8 — Frontend: Protezione Route e Navigazione Dinamica

**Obiettivo**: Route guard per pagine, UI navbar coerente con ruolo utente.

**Attività**:

1. Creare `wwwroot/js/route-guard.js`:
   - Definire mappa pagina → ruoli ammessi:

   ```javascript
     const PAGE_PERMISSIONS = {
       '/index.html': ['*'],
       '/programmazione.html': ['*'],
       '/login.html': ['anonimo'],
       '/registrazione.html': ['anonimo'],
       '/profilo.html': ['User', 'PowerUser', 'Admin'],
       '/dashboard.html': ['PowerUser', 'Admin'],
       '/films.html': ['PowerUser', 'Admin'],
       '/registi.html': ['PowerUser', 'Admin'],
       '/cinemas.html': ['PowerUser', 'Admin'],
       '/proiezioni.html': ['PowerUser', 'Admin'],
       '/categorie.html': ['PowerUser', 'Admin']
     };
     ```

   - Funzione `checkPageAccess()`:
     - Se pagina richiede auth e utente non loggato → redirect `/login.html?redirect=<pagina>`
     - Se pagina è solo per anonimo e utente loggato → redirect `/index.html`
     - Se utente non ha il ruolo richiesto → redirect `/index.html` (o `/profilo.html` se loggato)
   - Eseguire al caricamento di ogni pagina (prima del render)

2. Aggiornare `wwwroot/components/navbar-landing.html`:
   - Mostrare/nascondere link in base a ruolo:
     - Anonimo: "Accedi", "Programmazione"
     - User: "Programmazione", "Profilo", nome utente, "Logout"
     - PowerUser/Admin: + link "Area Admin" → `/dashboard.html`

3. Aggiornare `wwwroot/components/navbar-admin.html`:
   - Mostrare link "Categorie" nel menu
   - Aggiornare avatar con iniziali reali utente
   - Aggiungere link "Profilo" e "Logout" nel dropdown utente
   - Rimuovere `#login-btn` mock

4. Aggiornare `wwwroot/dashboard.html`:
   - Sostituire sidebar hardcoded con componente dinamico
   - Aggiungere link "Profilo" e "Logout" nella sidebar
   - Includere `auth.js` e `route-guard.js`

5. Aggiornare `wwwroot/js/navbar.js`:
   - Rimuovere `mockLogin()`, `mockLogout()`, `updateAuthUI()` mock
   - Sostituire con logica che usa `auth.js` (getCurrentUser, getUserRole, logout)

6. Includere `auth.js` e `route-guard.js` in **tutte** le pagine HTML (dopo `utils.js`, prima degli script pagina)

**Verifica**: Utente non autenticato non accede a pagine admin (redirect login). User non vede "Area Admin". Redirect corretti su accesso diretto URL.

---

### FASE 9 — Frontend: Pagina Programmazione Pubblica e Gestione Categorie Admin

**Obiettivo**: Nuova pagina pubblica programmazione (film + proiezioni con filtri), pagina admin CRUD categorie, aggiornamento pagina film con categorie.

**Attività**:

1. Creare `wwwroot/programmazione.html`:
   - Tema scuro (landing style)
   - Hero section "Programmazione Corrente"
   - Filtri: Città (dropdown dinamico), Data (date picker), Categoria (dropdown dinamico)
   - Griglia card film con: copertina, titolo, durata, categorie (badge), proiezioni (cinema + data + ora)
   - Bottone "Prenota" per ogni proiezione:
     - Se anonimo → redirect `/login.html?redirect=/programmazione.html`
     - Se loggato → redirect `/profilo.html?prenota=<proiezioneId>`

2. Creare `wwwroot/js/pages/programmazione.js`:
   - Caricare film, proiezioni, cinema, categorie in parallelo
   - Join client-side: per ogni film trovare proiezioni associate + cinema
   - Applicare filtri (città, data, categoria)
   - Gestire click "Prenota" auth-aware

3. Creare `wwwroot/categorie.html`:
   - Tema admin (light)
   - Tabella categorie con CRUD (create/edit/delete modali)
   - Stat: numero totale categorie

4. Creare `wwwroot/js/pages/categorie.js`:
   - CRUD completo categorie via `API.getCategorie()`, `API.createCategoria()`, etc.
   - Validazione nome non vuoto

5. Aggiornare `wwwroot/index.html`:
   - Sezione "Programmazione" con link a `/programmazione.html`
   - Card film mostrano categorie come badge
   - Bottone "Prenota" auth-aware (come programmazione.html)

6. Aggiornare `wwwroot/js/pages/home.js`:
   - Caricare categorie e associare ai film
   - Mostrare categorie come badge nelle card

7. Aggiornare `wwwroot/films.html` e `wwwroot/js/pages/films.js`:
   - Modal create/edit film: aggiungere multi-select/checkbox per categorie
   - Tabella film: colonna "Categorie" con badge
   - Filtro tabella per categoria

8. Aggiornare `wwwroot/components/navbar-landing.html`:
   - Aggiungere link "Programmazione" visibile a tutti

**Verifica**: Pagina pubblica programmazione funzionante con filtri. CRUD categorie admin OK. Film con categorie visibili e gestibili.

---

### FASE 10 — Frontend: Area Personale Utente (Profilo e Prenotazioni)

**Obiettivo**: Pagina profilo utente con dati contatto, lista prenotazioni e form nuova prenotazione.

**Attività**:

1. Creare `wwwroot/profilo.html`:
   - Tema landing (dark) o admin (light) — coerente con navbar utente
   - Sezione "Dati Personali": Nome, Cognome, Email (readonly), Telefono, bottone "Modifica"
   - Sezione "Le Mie Prenotazioni": tabella con Film, Cinema, Data, Ora, Posti, Note, azione "Annulla"
   - Sezione/popup "Nuova Prenotazione": form con Proiezione (select), NumeroPosti, Note
   - Stat: numero prenotazioni attive

2. Creare `wwwroot/js/pages/profilo.js`:
   - Caricare profilo utente (`API.getProfilo()`)
   - Caricare prenotazioni utente (`API.getPrenotazioni()`)
   - Gestione modifica profilo (inline edit o modal)
   - Gestione annullamento prenotazione (`API.deletePrenotazione()`)
   - Gestione nuova prenotazione:
     - Se URL ha `?prenota=<proiezioneId>` → aprire form con proiezione pre-selezionata
     - Altrimenti form libero con select proiezioni
   - Chiamata `API.createPrenotazione()`
   - Feedback toast per ogni operazione

3. Aggiornare `wwwroot/components/navbar-landing.html`:
   - Se loggato come User: link "Il Mio Profilo" nel dropdown utente

4. Aggiornare `wwwroot/programmazione.html`:
   - Bottone "Prenota" redirect a `/profilo.html?prenota=<proiezioneId>`

**Verifica**: User modifica profilo, crea/annulla prenotazioni. Flusso programmazione → prenota → profilo end-to-end.

---

### FASE 11 — Verifica Finale, Documentazione e Pulizia

**Obiettivo**: Chiusura iterazione con tutti i test verdi, verifica manuale flussi, documentazione aggiornata.

**Attività**:

1. Eseguire test backend completi: `dotnet test tests/backend/FilmAPI.Tests.csproj`

2. Verifica manuale flussi per ogni ruolo:

   **Admin:**
   - Login → dashboard → CRUD tutto (film, registi, cinema, proiezioni, categorie)
   - Gestione utenti (cambio ruolo)
   - Visualizzazione tutte prenotazioni

   **PowerUser:**
   - Login → dashboard → CRUD film/registi/proiezioni/categorie
   - Cinema: solo lettura (no create/update/delete)
   - Non può accedere gestione utenti

   **User:**
   - Registrazione → login → naviga programmazione → prenota → gestisce profilo/prenotazioni
   - Non può accedere dashboard/films/registi/cinemas/proiezioni/categorie (redirect)
   - Non vede bottone "Area Admin"

   **Anonimo:**
   - Naviga index + programmazione (sola lettura)
   - Click "Prenota" → redirect login
   - Non accede a pagine protette (redirect login)

3. Verifica redirect:
   - URL diretto a `/dashboard.html` senza login → redirect login
   - URL diretto a `/films.html` come User → redirect index
   - URL diretto a `/login.html` già loggato → redirect index

4. Aggiornare `docs/project/status.md` con stato aggiornato

5. Aggiornare `docs/project/changelog.md` con tutte le modifiche

6. Aggiornare `docs/project/dev_iteration/3-alternative/PianoDiLavoro.md` (questo file) con eventuali deviazioni implementative

---

## 5) Riepilogo Nuovi File

### Backend (`backend/FilmAPI/`)

| File | Descrizione |
|---|---|
| `Model/UserRole.cs` | Enum: User=0, PowerUser=1, Admin=2 |
| `Model/User.cs` | Entità utente |
| `Model/RefreshToken.cs` | Entità refresh token |
| `Model/Prenotazione.cs` | Entità prenotazione virtuale |
| `Model/Categoria.cs` | Entità categoria film |
| `Model/FilmCategoria.cs` | Tabella ponte Film↔Categoria |
| `DTO/CategoriaDTO.cs` | DTO lettura categoria |
| `DTO/CategoriaCreateDTO.cs` | DTO creazione categoria |
| `DTO/CategoriaUpdateDTO.cs` | DTO aggiornamento categoria |
| `DTO/LoginRequestDTO.cs` | DTO login |
| `DTO/RegisterRequestDTO.cs` | DTO registrazione |
| `DTO/AuthResponseDTO.cs` | DTO risposta auth (token + user) |
| `DTO/UserInfoDTO.cs` | DTO info utente |
| `DTO/RefreshTokenRequestDTO.cs` | DTO richiesta refresh |
| `DTO/ProfiloUpdateDTO.cs` | DTO aggiornamento profilo |
| `DTO/PrenotazioneDTO.cs` | DTO lettura prenotazione |
| `DTO/PrenotazioneCreateDTO.cs` | DTO creazione prenotazione |
| `DTO/UserAdminDTO.cs` | DTO utente per admin |
| `DTO/UpdateRuoloDTO.cs` | DTO cambio ruolo |
| `Services/IAuthService.cs` | Interfaccia auth |
| `Services/AuthService.cs` | Implementazione auth |
| `Services/ICategoriaService.cs` | Interfaccia categorie |
| `Services/CategoriaService.cs` | Implementazione categorie |
| `Services/IProfiloService.cs` | Interfaccia profilo |
| `Services/ProfiloService.cs` | Implementazione profilo |
| `Services/IPrenotazioneService.cs` | Interfaccia prenotazioni |
| `Services/PrenotazioneService.cs` | Implementazione prenotazioni |
| `Services/IUserAdminService.cs` | Interfaccia admin utenti |
| `Services/UserAdminService.cs` | Implementazione admin utenti |
| `Endpoints/AuthEndpoints.cs` | Endpoint /auth/* |
| `Endpoints/CategorieEndpoints.cs` | Endpoint /categorie/* |
| `Endpoints/ProfiloEndpoints.cs` | Endpoint /profilo |
| `Endpoints/PrenotazioniEndpoints.cs` | Endpoint /prenotazioni/* |
| `Endpoints/AdminUtentiEndpoints.cs` | Endpoint /admin/utenti/* |

### Frontend (`frontend/CineBase.Web/wwwroot/`)

| File | Descrizione |
|---|---|
| `login.html` | Pagina login |
| `registrazione.html` | Pagina registrazione |
| `programmazione.html` | Pagina programmazione pubblica |
| `profilo.html` | Area personale utente |
| `categorie.html` | Gestione categorie admin |
| `js/auth.js` | Modulo autenticazione + token management |
| `js/route-guard.js` | Protezione route per ruolo |
| `js/pages/login.js` | Logica pagina login |
| `js/pages/registrazione.js` | Logica pagina registrazione |
| `js/pages/programmazione.js` | Logica programmazione pubblica |
| `js/pages/profilo.js` | Logica area personale |
| `js/pages/categorie.js` | Logica CRUD categorie |

### File modificati (principali)

| File | Modifiche |
|---|---|
| `backend/FilmAPI/Model/Film.cs` | Navigation `FilmCategorie` |
| `backend/FilmAPI/Data/FilmDbContext.cs` | Nuovi DbSet, relazioni, indici |
| `backend/FilmAPI/Program.cs` | JWT config, middleware auth, policy, DI nuovi servizi, mapping nuovi endpoint |
| `backend/.env` + `backend/.env.example` | Variabili JWT e seed |
| `backend/FilmAPI/DTO/FilmDTO.cs` | Campo `Categorie` |
| `backend/FilmAPI/DTO/FilmCreateDTO.cs` | Campo `CategorieIds` |
| `backend/FilmAPI/DTO/FilmUpdateDTO.cs` | Campo `CategorieIds` |
| `backend/FilmAPI/Services/FilmService.cs` | Gestione categorie in CRUD |
| `backend/FilmAPI/Endpoints/*.cs` | `RequireAuthorization` / `AllowAnonymous` |
| `frontend/.../js/api.js` | Token header, refresh interceptor, nuovi metodi |
| `frontend/.../js/navbar.js` | Rimozione mock auth, usa auth.js |
| `frontend/.../components/navbar-landing.html` | Menu auth-aware |
| `frontend/.../components/navbar-admin.html` | Link categorie, logout reale |
| `frontend/.../index.html` | Link programmazione, include auth.js |
| `frontend/.../dashboard.html` | Sidebar aggiornata, include auth.js |
| `frontend/.../films.html` | Categorie in UI, include auth.js |
| `frontend/.../js/pages/films.js` | CRUD con categorie |
| `frontend/.../*.html` | Tutte includono auth.js + route-guard.js |
| `tests/backend/CustomWebApplicationFactory.cs` | Helper client autenticati |
| `tests/backend/Integration/ApiIntegrationTests.cs` | Test aggiornati + nuovi |

---

## 6) Criteri di Accettazione

L'iterazione è completata quando TUTTE le seguenti condizioni sono vere:

1. Un anonimo naviga `index.html` e `programmazione.html` (sola lettura) ma non può prenotare
2. Un utente può registrarsi e fare login, ricevendo JWT validi
3. Il refresh token rinnova l'access token senza nuovo login
4. Un `User` prenota proiezioni e gestisce il proprio profilo
5. Un `User` non accede alle pagine admin (redirect automatico)
6. Un `PowerUser` fa CRUD su Film/Proiezioni/Registi/Categorie ma solo Read su Cinema
7. Un `Admin` fa tutto, inclusa gestione ruoli utenti
8. Le categorie sono gestibili e visibili sui film (badge, filtri)
9. La programmazione pubblica è filtrabile per città, data e categoria
10. La navbar mostra elementi coerenti con il ruolo utente
11. Le API restituiscono 401/403 coerenti sugli accessi non autorizzati
12. Tutti i test backend passano (vecchi + nuovi)
13. I redirect funzionano per tutti i casi di accesso non autorizzato

---

## 7) Prompt Guida per AI (per fase)

**Fase 1**: "Implementa Fase 1: crea modelli UserRole, User, RefreshToken, Prenotazione, Categoria, FilmCategoria. Aggiorna Film con FilmCategorie. Aggiorna FilmDbContext. Installa BCrypt.Net-Next e JwtBearer. Configura JWT in Program.cs (senza attivare middleware auth). Aggiorna .env. Crea migration. Implementa seed admin e 12 categorie. Verifica test esistenti verdi."

**Fase 2**: "Implementa Fase 2: crea CategoriaService CRUD, DTO categorie, aggiorna FilmDTO/FilmCreateDTO/FilmUpdateDTO per categorie, aggiorna FilmService per CategorieIds, crea CategorieEndpoints. Testa via Swagger."

**Fase 3**: "Implementa Fase 3: crea AuthService (register, login, refresh, logout), DTO auth, AuthEndpoints. Registra servizio e mappa endpoint. Testa via Swagger."

**Fase 4**: "Implementa Fase 4: attiva UseAuthentication/UseAuthorization in Program.cs. Definisci policy AdminOnly, PowerUserOrAdmin. Applica RequireAuthorization/AllowAnonymous a tutti gli endpoint secondo matrice permessi. Aggiorna CORS per Authorization header."

**Fase 5**: "Implementa Fase 5: crea ProfiloService, PrenotazioneService, UserAdminService. Crea endpoint /profilo, /prenotazioni, /admin/utenti con autorizzazione appropriata. Testa via Swagger."

**Fase 6**: "Implementa Fase 6: aggiorna CustomWebApplicationFactory con helper client autenticati. Aggiorna test esistenti per auth. Aggiungi test A1-A8 (auth), RB1-RB8 (RBAC), CAT1-CAT5 (categorie), PR1-PR5 (prenotazioni). Esegui dotnet test e verifica tutti verdi."

**Fase 7**: "Implementa Fase 7: crea auth.js (token management), aggiorna api.js (Bearer header + refresh interceptor), crea login.html + login.js, registrazione.html + registrazione.js. Aggiorna navbar-landing con auth-aware UI."

**Fase 8**: "Implementa Fase 8: crea route-guard.js (mappa permessi pagine + redirect). Aggiorna navbar-landing, navbar-admin, dashboard sidebar per navigazione auth-aware. Aggiorna navbar.js. Includi auth.js e route-guard.js in tutte le pagine."

**Fase 9**: "Implementa Fase 9: crea programmazione.html con filtri città/data/categoria e bottone prenota auth-aware. Crea categorie.html con CRUD admin. Aggiorna films.html per categorie (badge + multi-select). Aggiorna index.html con link programmazione."

**Fase 10**: "Implementa Fase 10: crea profilo.html con dati personali, lista prenotazioni e form nuova prenotazione. Supporta ?prenota=id da programmazione. Gestisci modifica profilo e annullamento prenotazioni."

**Fase 11**: "Esegui verifica finale: dotnet test tutti verdi, verifica manuale flussi Admin/PowerUser/User/Anonimo, aggiorna status.md e changelog.md."
