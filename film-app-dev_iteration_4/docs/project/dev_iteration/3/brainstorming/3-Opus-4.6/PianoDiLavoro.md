# Piano di Lavoro - Iterazione 3: Autenticazione, Autorizzazione RBAC, Categorie Film e Area Personale

Autore: OpenCode con Opus 4.6

## 1) Panoramica

Questa iterazione mette in sicurezza le API del backend CineBase e introduce nuove funzionalita:

- Categorie Film con relazione many-to-many e CRUD completo
- Autenticazione JWT con access token (breve durata) + refresh token (lunga durata)
- RBAC (Role-Based Access Control) con 4 livelli: Admin, PowerUser, User, Anonimo
- Area personale utente con dati di contatto e prenotazioni virtuali
- Pagina pubblica programmazione per utenti non autenticati
- Protezione route frontend con redirect basato su ruolo

### 1.1 Contesto Architetturale

```text
repo-root/
|- backend/FilmAPI/          (API .NET 9, porta 5000)
|- frontend/CineBase.Web/    (Static files, porta 5001)
|- tests/backend/            (71 test esistenti)
`- docs/
```

Stato attuale: API completamente aperte, autenticazione mock solo lato client (`sessionStorage`), 71 test backend verdi. Nessuna entita Categoria presente.

---

## 2) Ruoli e Matrice dei Permessi

### 2.1 Definizione Ruoli

| Ruolo | Descrizione |
| --- | --- |
| Admin | Massimo privilegio. CRUD su tutte le entita, gestione utenti, accesso a tutte le pagine. |
| PowerUser | CRUD su Film, Proiezioni, Registi, Categorie. Solo Read su Cinema (no Create/Update/Delete). |
| User | Utente autenticato. Consulta programmazione, salva prenotazioni, gestisce profilo. Non accede all'area admin. |
| Anonimo | Non autenticato. Vede `index.html` e `programmazione.html` (solo lettura). Redirect a login se prova a prenotare. |

### 2.2 Matrice Permessi API

| Endpoint | Anonimo | User | PowerUser | Admin |
| --- | --- | --- | --- | --- |
| **Auth** | | | | |
| `POST /auth/register` | SI | - | - | - |
| `POST /auth/login` | SI | - | - | - |
| `POST /auth/refresh` | SI | - | - | - |
| `POST /auth/logout` | - | SI | SI | SI |
| `GET /auth/me` | - | SI | SI | SI |
| **Categorie** | | | | |
| `GET /categorie` | SI | SI | SI | SI |
| `GET /categorie/{id}` | SI | SI | SI | SI |
| `POST /categorie` | - | - | SI | SI |
| `PUT /categorie/{id}` | - | - | SI | SI |
| `DELETE /categorie/{id}` | - | - | SI | SI |
| **Film** | | | | |
| `GET /films` | SI | SI | SI | SI |
| `GET /films/{id}` | SI | SI | SI | SI |
| `POST /films` | - | - | SI | SI |
| `PUT /films/{id}` | - | - | SI | SI |
| `DELETE /films/{id}` | - | - | SI | SI |
| **Registi** | | | | |
| `GET /registi` | - | - | SI | SI |
| `GET /registi/{id}` | - | - | SI | SI |
| `POST /registi` | - | - | SI | SI |
| `PUT /registi/{id}` | - | - | SI | SI |
| `DELETE /registi/{id}` | - | - | SI | SI |
| `GET /registi/{id}/films` | - | - | SI | SI |
| `POST /registi/{id}/films` | - | - | SI | SI |
| **Cinema** | | | | |
| `GET /cinemas` | SI | SI | SI | SI |
| `GET /cinemas/{id}` | SI | SI | SI | SI |
| `POST /cinemas` | - | - | - | SI |
| `PUT /cinemas/{id}` | - | - | - | SI |
| `DELETE /cinemas/{id}` | - | - | - | SI |
| **Proiezioni** | | | | |
| `GET /proiezioni` | SI | SI | SI | SI |
| `GET /proiezioni/{id}` | SI | SI | SI | SI |
| `POST /proiezioni` | - | - | SI | SI |
| `PUT /proiezioni/{id}` | - | - | SI | SI |
| `DELETE /proiezioni/{id}` | - | - | SI | SI |
| **Media** | | | | |
| `POST /media/covers` | - | - | SI | SI |
| **Profilo** | | | | |
| `GET /profilo` | - | SI | SI | SI |
| `PUT /profilo` | - | SI | SI | SI |
| **Prenotazioni** | | | | |
| `GET /prenotazioni` | - | SI (proprie) | - | SI (tutte) |
| `POST /prenotazioni` | - | SI | - | - |
| `DELETE /prenotazioni/{id}` | - | SI (proprie) | - | SI |
| **Admin Utenti** | | | | |
| `GET /admin/utenti` | - | - | - | SI |
| `PUT /admin/utenti/{id}/ruolo` | - | - | - | SI |

> Nota: `GET /films`, `GET /cinemas`, `GET /proiezioni`, `GET /categorie` sono pubblici per supportare `programmazione.html` e `index.html`.

### 2.3 Matrice Permessi Pagine Frontend

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

---

## 3) Design Tecnico

### 3.1 Nuove Entita

**Categoria:**

```text
Categoria(Id, Nome[unique, required, max 100])
```

**FilmCategoria (tabella ponte esplicita):**

```text
FilmCategoria(FilmId[FK], CategoriaId[FK]) -- PK composita
```

**User:**

```text
User(Id, Email[unique], PasswordHash, Nome, Cognome, Telefono?, Ruolo[enum], DataRegistrazione)
```

**UserRole (enum):**

```text
User = 0, PowerUser = 1, Admin = 2
```

**RefreshToken:**

```text
RefreshToken(Id, Token[unique], UserId[FK], ExpiresAt, CreatedAt, RevokedAt?)
```

**Prenotazione:**

```text
Prenotazione(Id, UserId[FK], ProiezioneId[FK], NumeroPosti, Note?, DataPrenotazione)
```

### 3.2 Relazioni

- Categoria M-N Film (tramite FilmCategoria)
- User 1-N RefreshToken
- User 1-N Prenotazione
- Proiezione 1-N Prenotazione

### 3.3 JWT Token Design

| Parametro | Access Token | Refresh Token |
| --- | ---  | --- |
| Durata | 15 minuti | 7 giorni |
| Contenuto | `userId`, `email`, `ruolo` (claim) | stringa random (GUID) |
| Storage frontend | `localStorage` | `localStorage` |
| Rinnovamento | via refresh token | rotazione: vecchio revocato, nuovo emesso |

### 3.4 Pacchetti NuGet da Aggiungere

- `Microsoft.AspNetCore.Authentication.JwtBearer` 9.x
- `BCrypt.Net-Next` (hashing password)

### 3.5 Variabili `.env` Nuove

```env
JWT_SECRET=<chiave segreta min 256 bit>
JWT_ISSUER=CineBaseAPI
JWT_AUDIENCE=CineBaseWeb
JWT_ACCESS_TOKEN_EXPIRY_MINUTES=15
JWT_REFRESH_TOKEN_EXPIRY_DAYS=7
ADMIN_SEED_EMAIL=admin@cinebase.it
ADMIN_SEED_PASSWORD=Admin123!
```

### 3.6 Seed Categorie Iniziali

```text
Drammatico, Commedia, Avventura, Fantasy, Horror, Azione,
Fantascienza, Thriller, Animazione, Documentario, Romantico, Storico
```

---

## 4) Fasi di Implementazione

### Fase 1: Modello Dati Completo e Infrastruttura (Backend)

**Obiettivo**: creare nuove entita (`Categoria`, `FilmCategoria`, `User`, `RefreshToken`, `Prenotazione`), aggiornare DbContext, migration, config JWT e seed di admin + categorie.

**Attivita**:

1. Installare pacchetti NuGet:
   - `Microsoft.AspNetCore.Authentication.JwtBearer`
   - `BCrypt.Net-Next`
2. Creare `Model/Categoria.cs`:
   - `Id` (int, PK)
   - `Nome` (string, required, unique, max 100)
   - Navigation: `ICollection<FilmCategoria>`
3. Creare `Model/FilmCategoria.cs`:
   - `FilmId` (FK, parte della PK composita)
   - `CategoriaId` (FK, parte della PK composita)
   - Navigation: `Film`, `Categoria`
4. Aggiornare `Model/Film.cs`:
   - Navigation: `ICollection<FilmCategoria> FilmCategorie`
5. Creare `Model/UserRole.cs`:
   - `User = 0`, `PowerUser = 1`, `Admin = 2`
6. Creare `Model/User.cs`:
   - `Id`, `Email`, `PasswordHash`, `Nome`, `Cognome`, `Telefono`, `Ruolo`, `DataRegistrazione`
   - Navigation: `ICollection<RefreshToken>`, `ICollection<Prenotazione>`
7. Creare `Model/RefreshToken.cs`:
   - `Id`, `Token`, `UserId`, `ExpiresAt`, `CreatedAt`, `RevokedAt`
   - Proprieta computed `IsActive`
8. Creare `Model/Prenotazione.cs`:
   - `Id`, `UserId`, `ProiezioneId`, `NumeroPosti`, `Note`, `DataPrenotazione`
9. Aggiornare `Data/FilmDbContext.cs`:
   - Nuovi DbSet: `Categoria`, `FilmCategoria`, `User`, `RefreshToken`, `Prenotazione`
   - Fluent API: PK composita `FilmCategoria(FilmId, CategoriaId)`
   - Fluent API: unique index su `Categoria.Nome`, `User.Email`, `RefreshToken.Token`
   - Relazioni: Film-Categoria (M-N), User-RefreshToken (cascade), User-Prenotazione (cascade), Proiezione-Prenotazione (restrict)
10. Aggiornare `backend/.env` e `backend/.env.example`
11. Configurare JWT in `backend/FilmAPI/Program.cs`:
    - `AddAuthentication(...).AddJwtBearer(...)`
    - leggere chiave/issuer/audience da env
    - **senza** attivare ancora `UseAuthentication()`/`UseAuthorization()` (fase 4)
12. Creare migration:

      ```bash
      dotnet ef migrations add AddCategorieAndAuth
      ```

13. Implementare seed avvio app:
    - utente admin da `ADMIN_SEED_EMAIL` / `ADMIN_SEED_PASSWORD`
    - categorie iniziali se tabella vuota

14. Applicare migration e verificare seed

**Verifica**: migration applicata, admin nel DB, 12 categorie presenti, test esistenti verdi (`71/71`).

---

### Fase 2: Servizio e Endpoint Categorie (Backend)

**Obiettivo**: CRUD completo categorie + aggiornamento Film per supportare categorie multiple.

**Attivita**:

1. Creare DTO:
   - `CategoriaDTO` (`Id`, `Nome`)
   - `CategoriaCreateDTO` (`Nome`)
   - `CategoriaUpdateDTO` (`Nome`)
2. Aggiornare DTO Film:
   - `FilmDTO`: `Categorie: List<CategoriaDTO>`
   - `FilmCreateDTO`: `CategorieIds: List<int>?`
   - `FilmUpdateDTO`: `CategorieIds: List<int>?`
3. Creare `Services/CategoriaService.cs` (`ICategoriaService` + implementazione):
   - `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`
   - validazione duplicati nome
4. Aggiornare `Services/FilmService.cs`:
   - create/update: gestione `CategorieIds`
   - read: include `FilmCategorie` + `Categoria`
5. Creare `Endpoints/CategorieEndpoints.cs`:
   - `GET /categorie`
   - `GET /categorie/{id}`
   - `POST /categorie`
   - `PUT /categorie/{id}`
   - `DELETE /categorie/{id}`
6. Registrare `ICategoriaService` in `Program.cs` e mappare endpoint
7. Aggiungere link `Categorie` nella navbar admin (preparazione frontend)

**Verifica**: Swagger OK per CRUD categorie, film con categorie create/update/read.

---

### Fase 3: Servizio Autenticazione e Endpoint Auth (Backend)

**Obiettivo**: implementare register/login/refresh/logout/me.

**Attivita**:

1. Creare DTO auth:
   - `LoginRequestDTO`
   - `RegisterRequestDTO`
   - `AuthResponseDTO`
   - `UserInfoDTO`
   - `RefreshTokenRequestDTO`
2. Creare `Services/AuthService.cs` (`IAuthService` + implementazione):
   - `RegisterAsync`
   - `LoginAsync`
   - `RefreshAsync`
   - `LogoutAsync`
   - `GetUserByIdAsync`
   - helper: `GenerateAccessToken`, `GenerateRefreshToken`
3. Creare `Endpoints/AuthEndpoints.cs`:
   - `POST /auth/register`
   - `POST /auth/login`
   - `POST /auth/refresh`
   - `POST /auth/logout`
   - `GET /auth/me`
4. Registrare servizio e mappare endpoint in `Program.cs`

Nota: in questa fase middleware auth non ancora attivo; `logout`/`me` possono leggere header token in modo manuale per verifica preliminare.

**Verifica**: register/login/refresh funzionanti, login errato ritorna errore coerente.

---

### Fase 4: Middleware Autorizzazione e RBAC su Tutti gli Endpoint (Backend)

**Obiettivo**: attivare JWT middleware e applicare policy a tutti gli endpoint.

**Attivita**:

1. Attivare middleware in `Program.cs` (ordine):

   ```csharp
   app.UseCors(...);
   app.UseAuthentication();
   app.UseAuthorization();
   app.UseStaticFiles();
   ```

2. Definire policy:

   - `AdminOnly`
   - `PowerUserOrAdmin`
   - `Authenticated`
3. Applicare autorizzazioni:
   - **Categorie**: GET anonimi, CUD `PowerUserOrAdmin`
   - **Registi**: tutto `PowerUserOrAdmin`
   - **Films**: GET anonimi, CUD `PowerUserOrAdmin`
   - **Cinemas**: GET anonimi, CUD `AdminOnly`
   - **Proiezioni**: GET anonimi, CUD `PowerUserOrAdmin`
   - **Media**: upload `PowerUserOrAdmin`
   - **Auth**: register/login/refresh anonimi, logout/me autenticati
4. Verificare CORS per header `Authorization`

**Verifica**: 401/403/201 coerenti con matrice RBAC.

---

### Fase 5: Area Personale, Prenotazioni e Gestione Utenti (Backend)

**Obiettivo**: endpoint profilo, prenotazioni, gestione utenti admin.

**Attivita**:

1. Creare DTO:
   - `ProfiloUpdateDTO`
   - `PrenotazioneDTO`
   - `PrenotazioneCreateDTO`
   - `UserAdminDTO`
   - `UpdateRuoloDTO`
2. Creare `Services/ProfiloService.cs`
3. Creare `Services/PrenotazioneService.cs`
4. Creare `Services/UserAdminService.cs`
5. Creare endpoint:
   - `Endpoints/ProfiloEndpoints.cs` (`GET/PUT /profilo`)
   - `Endpoints/PrenotazioniEndpoints.cs` (`GET/POST/DELETE /prenotazioni`)
   - `Endpoints/AdminUtentiEndpoints.cs` (`GET`, `PUT ruolo`)
6. Registrare servizi e mappare endpoint in `Program.cs`

**Verifica**: user crea/gestisce solo proprie prenotazioni, admin gestisce utenti e legge tutte le prenotazioni.

---

### Fase 6: Aggiornamento Test Backend (Backend)

**Obiettivo**: aggiornare test esistenti con auth e aggiungere test nuovi (auth, RBAC, categorie, prenotazioni).

**Attivita**:

1. Aggiornare `tests/backend/CustomWebApplicationFactory.cs` con helper client autenticati:
   - `CreateAdminClient()`
   - `CreatePowerUserClient()`
   - `CreateUserClient()`
   - `CreateAnonymousClient()`
2. Aggiornare test integrazione esistenti a ruoli corretti
3. Aggiungere test auth (`A1`-`A8`)
4. Aggiungere test RBAC (`RBAC1`-`RBAC8`)
5. Aggiungere test categorie (`CAT1`-`CAT5`)
6. Aggiungere test prenotazioni (`PR1`-`PR5`)
7. Eseguire:

```bash
dotnet test tests/backend/FilmAPI.Tests.csproj
```

**Verifica**: test suite tutta verde.

---

### Fase 7: Frontend - Autenticazione e Gestione Token

**Obiettivo**: login/registrazione reali, token management, API client con refresh automatico.

**Attivita**:

1. Creare `frontend/CineBase.Web/wwwroot/js/auth.js`:
   - `getAccessToken`, `getRefreshToken`, `saveTokens`, `clearTokens`
   - `isLoggedIn`, `getCurrentUser`, `getUserRole`
   - `login`, `register`, `logout`, `refreshAccessToken`
2. Aggiornare `frontend/CineBase.Web/wwwroot/js/api.js`:
   - header auth automatico
   - retry su 401 con refresh
   - fallback logout+redirect se refresh fallisce
   - metodi auth/profilo/prenotazioni/categorie
3. Creare `login.html` + `js/pages/login.js`
4. Creare `registrazione.html` + `js/pages/registrazione.js`

**Verifica**: login/register funzionanti, token salvati, chiamate protette OK.

---

### Fase 8: Frontend - Protezione Route e Navigazione Dinamica

**Obiettivo**: autorizzazione lato pagina + UI dinamica per ruolo.

**Attivita**:

1. Creare `frontend/CineBase.Web/wwwroot/js/route-guard.js`:
   - mappa pagine/permessi
   - redirect login con `?redirect=`
   - redirect su accesso vietato
2. Aggiornare `components/navbar-landing.html`
3. Aggiornare `components/navbar-admin.html`
4. Aggiornare `dashboard.html` (sidebar utente/links/logout)
5. Aggiornare `js/navbar.js` rimuovendo mock auth
6. Includere `auth.js` e `route-guard.js` in tutte le pagine

**Verifica**: redirect e visibilita menu coerenti con ruolo.

---

### Fase 9: Frontend - Programmazione Pubblica e Categorie Admin

**Obiettivo**: nuova pagina pubblica programmazione + pagina admin categorie.

**Attivita**:

1. Creare `programmazione.html` + `js/pages/programmazione.js`:
   - join dati proiezioni/film/cinema/categorie
   - filtri per citta/data/categoria
   - prenota con redirect auth-aware
2. Creare `categorie.html` + `js/pages/categorie.js` (CRUD)
3. Aggiornare `index.html` con link a programmazione
4. Aggiornare `navbar-landing.html` link Programmazione
5. Aggiornare `films.html` + `js/pages/films.js`:
   - categorie come badge
   - multi-select/checkbox categorie
   - filtro per categoria

**Verifica**: pagine pubbliche e admin categorie funzionanti.

---

### Fase 10: Frontend - Area Personale Utente

**Obiettivo**: profilo utente con contatti e prenotazioni virtuali.

**Attivita**:

1. Creare `profilo.html` + `js/pages/profilo.js`:
   - sezione dati personali (nome/cognome/email/telefono)
   - modifica profilo
   - lista prenotazioni
   - annullo prenotazione
   - form nuova prenotazione
2. Gestire query `?prenota=<proiezioneId>` da programmazione
3. Arricchire dati prenotazioni lato client

**Verifica**: utente gestisce profilo e prenotazioni end-to-end.

---

### Fase 11: Verifica Finale e Documentazione

**Obiettivo**: chiusura iterazione con test verdi e docs aggiornate.

**Attivita**:

1. Eseguire test backend completi
2. Verifica manuale flussi:
   - Admin
   - PowerUser
   - User
   - Anonimo
3. Aggiornare `docs/project/status.md`
4. Aggiornare `docs/project/changelog.md`

---

## 5) Nuovi File da Creare (Riepilogo)

### Backend

| File | Descrizione |
| --- | --- |
| `Model/UserRole.cs` | Enum ruoli |
| `Model/User.cs` | Entita utente |
| `Model/RefreshToken.cs` | Entita refresh token |
| `Model/Prenotazione.cs` | Entita prenotazione |
| `Model/Categoria.cs` | Entita categoria |
| `Model/FilmCategoria.cs` | Tabella ponte film-categoria |
| `DTO/CategoriaDTO.cs` | DTO lettura categoria |
| `DTO/CategoriaCreateDTO.cs` | DTO creazione categoria |
| `DTO/CategoriaUpdateDTO.cs` | DTO aggiornamento categoria |
| `DTO/LoginRequestDTO.cs` | DTO login |
| `DTO/RegisterRequestDTO.cs` | DTO registrazione |
| `DTO/AuthResponseDTO.cs` | DTO risposta auth |
| `DTO/UserInfoDTO.cs` | DTO info utente |
| `DTO/RefreshTokenRequestDTO.cs` | DTO refresh |
| `DTO/ProfiloUpdateDTO.cs` | DTO update profilo |
| `DTO/PrenotazioneDTO.cs` | DTO lettura prenotazione |
| `DTO/PrenotazioneCreateDTO.cs` | DTO creazione prenotazione |
| `DTO/UserAdminDTO.cs` | DTO utente per admin |
| `DTO/UpdateRuoloDTO.cs` | DTO cambio ruolo |
| `Services/AuthService.cs` | Servizio autenticazione |
| `Services/CategoriaService.cs` | Servizio categorie |
| `Services/ProfiloService.cs` | Servizio profilo |
| `Services/PrenotazioneService.cs` | Servizio prenotazioni |
| `Services/UserAdminService.cs` | Servizio gestione utenti |
| `Endpoints/AuthEndpoints.cs` | Endpoint auth |
| `Endpoints/CategorieEndpoints.cs` | Endpoint categorie |
| `Endpoints/ProfiloEndpoints.cs` | Endpoint profilo |
| `Endpoints/PrenotazioniEndpoints.cs` | Endpoint prenotazioni |
| `Endpoints/AdminUtentiEndpoints.cs` | Endpoint admin utenti |

### Frontend

| File | Descrizione |
| --- | --- |
| `wwwroot/login.html` | Pagina login |
| `wwwroot/registrazione.html` | Pagina registrazione |
| `wwwroot/programmazione.html` | Pagina programmazione pubblica |
| `wwwroot/profilo.html` | Area personale |
| `wwwroot/categorie.html` | Gestione categorie admin |
| `wwwroot/js/auth.js` | Modulo autenticazione |
| `wwwroot/js/route-guard.js` | Protezione route |
| `wwwroot/js/pages/login.js` | Logica login |
| `wwwroot/js/pages/registrazione.js` | Logica registrazione |
| `wwwroot/js/pages/programmazione.js` | Logica programmazione |
| `wwwroot/js/pages/profilo.js` | Logica profilo |
| `wwwroot/js/pages/categorie.js` | Logica categorie |

### File Modificati (principali)

| File | Modifiche |
| --- | --- |
| `backend/FilmAPI/Model/Film.cs` | Navigation `FilmCategorie` |
| `backend/FilmAPI/Program.cs` | JWT config, middleware auth, policy, mapping endpoint |
| `backend/FilmAPI/Data/FilmDbContext.cs` | Nuovi DbSet, relazioni, indici |
| `backend/.env` e `backend/.env.example` | Variabili JWT e seed admin |
| `backend/FilmAPI/DTO/FilmDTO.cs` | Campo `Categorie` |
| `backend/FilmAPI/DTO/FilmCreateDTO.cs` | Campo `CategorieIds` |
| `backend/FilmAPI/DTO/FilmUpdateDTO.cs` | Campo `CategorieIds` |
| `backend/FilmAPI/Services/FilmService.cs` | Gestione categorie film |
| `backend/FilmAPI/Endpoints/*.cs` | `RequireAuthorization` / `AllowAnonymous` |
| `frontend/CineBase.Web/wwwroot/js/api.js` | Token header, refresh interceptor, nuovi metodi |
| `frontend/CineBase.Web/wwwroot/js/navbar.js` | Rimozione mock auth, UI ruolo-based |
| `frontend/CineBase.Web/wwwroot/components/navbar-landing.html` | Menu auth-aware |
| `frontend/CineBase.Web/wwwroot/components/navbar-admin.html` | Menu admin aggiornato + link categorie |
| `frontend/CineBase.Web/wwwroot/index.html` | Link programmazione + prenota auth-aware |
| `frontend/CineBase.Web/wwwroot/dashboard.html` | Sidebar aggiornata |
| `frontend/CineBase.Web/wwwroot/films.html` | Categorie film in UI |
| `frontend/CineBase.Web/wwwroot/js/pages/films.js` | CRUD film con categorie |
| `frontend/CineBase.Web/wwwroot/*.html` | include `auth.js` e `route-guard.js` |
| `tests/backend/CustomWebApplicationFactory.cs` | helper client autenticati |
| `tests/backend/Integration/ApiIntegrationTests.cs` | update test + nuovi scenari |

---

## 6) Criteri di Accettazione

L'iterazione e completata quando:

1. Un anonimo puo navigare `index.html` e `programmazione.html` ma non puo prenotare
2. Un utente puo registrarsi, fare login e ricevere token JWT validi
3. Il refresh token rinnova l'access token senza nuovo login
4. Un utente autenticato (`User`) puo prenotare proiezioni e gestire il profilo
5. Un utente `User` non puo accedere alle pagine amministrative (redirect)
6. Un `PowerUser` puo fare CRUD su Film/Proiezioni/Registi/Categorie ma solo Read su Cinema
7. Un `Admin` puo fare tutto, inclusa la gestione ruoli utenti
8. Le categorie sono gestibili e visibili sui film
9. La programmazione pubblica e filtrabile per citta, data e categoria
10. La navbar mostra elementi coerenti con ruolo
11. Le API restituiscono 401/403 coerenti sugli accessi non autorizzati
12. Tutti i test backend passano

---

## 7) Prompt Guida per AI (per fase)

**Fase 1:**

"Implementa Fase 1 dell'Iterazione 3: crea i modelli Categoria, FilmCategoria, User, UserRole, RefreshToken, Prenotazione nel backend. Aggiorna Film con navigation FilmCategorie. Aggiorna FilmDbContext. Installa BCrypt.Net-Next e Microsoft.AspNetCore.Authentication.JwtBearer. Configura JWT in Program.cs senza attivare middleware. Aggiorna .env. Crea migration AddCategorieAndAuth. Implementa seed admin e 12 categorie. Verifica 71 test esistenti verdi."

**Fase 2:**

"Implementa Fase 2: crea CategoriaService con CRUD completo. Crea DTO categoria. Aggiorna FilmDTO/FilmCreateDTO/FilmUpdateDTO per categorie. Aggiorna FilmService per gestire CategorieIds. Crea CategorieEndpoints. Testa via Swagger."

**Fase 3:**

"Implementa Fase 3: crea AuthService con register, login, refresh, logout. Crea DTO auth. Crea AuthEndpoints. Registra servizio e mappa endpoint. Testa via Swagger."

**Fase 4:**

"Implementa Fase 4: attiva UseAuthentication/UseAuthorization in Program.cs. Definisci policy AdminOnly, PowerUserOrAdmin, Authenticated. Applica RequireAuthorization e AllowAnonymous a tutti gli endpoint secondo la matrice permessi del piano."

**Fase 5:**

"Implementa Fase 5: crea ProfiloService, PrenotazioneService, UserAdminService. Crea endpoint /profilo, /prenotazioni, /admin/utenti con autorizzazione appropriata. Testa via Swagger."

**Fase 6:**

"Implementa Fase 6: aggiorna CustomWebApplicationFactory con helper client autenticati. Aggiorna test esistenti per usare auth. Aggiungi test A1-A8 (auth), RBAC1-RBAC8, CAT1-CAT5 (categorie), PR1-PR5 (prenotazioni). Esegui dotnet test e verifica tutti verdi."

**Fase 7:**

"Implementa Fase 7: crea auth.js e aggiorna api.js con header auth e interceptor refresh. Crea login.html e registrazione.html con JS associati. Tema scuro, validazione, redirect post-login."

**Fase 8:**

"Implementa Fase 8: crea route-guard.js con mappa permessi pagine. Aggiorna navbar-landing, navbar-admin, dashboard sidebar per navigazione auth-aware. Aggiorna navbar.js. Includi auth.js e route-guard.js in tutte le pagine."

**Fase 9:**

"Implementa Fase 9: crea programmazione.html con filtri per citta/data/categoria e bottone prenota auth-aware. Crea categorie.html admin con CRUD. Aggiorna films.html per mostrare/gestire categorie. Aggiorna index.html con link programmazione."

**Fase 10:**

"Implementa Fase 10: crea profilo.html con sezione dati personali, prenotazioni e form nuova prenotazione. Supporta parametro ?prenota=id da programmazione. Gestisci modifica profilo e annullamento prenotazioni."

**Fase 11:**

"Esegui verifica finale: dotnet test tutti verdi, verifica manuale flussi Admin/PowerUser/User/Anonimo, aggiorna status.md e changelog.md."
