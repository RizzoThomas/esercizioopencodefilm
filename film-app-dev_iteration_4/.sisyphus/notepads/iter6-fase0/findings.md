# FASE 0 — Findings: Infrastructure Inventory & Docker Prerequisites

> **Date:** 2026-05-27 | **Orchestrator:** Atlas

---

## 1. Service Inventory

| Service | Path | Framework | Port | Health Endpoint |
|---|---|---|---|---|
| Backend API | `backend/FilmAPI/` | .NET 9 ASP.NET | 5000 | `GET /health` |
| Frontend | `frontend/CineBase.Web/` | .NET 9 static server | 5001 | `GET /` (redirects to /index.html) |
| Database | N/A (containerized) | MariaDB 11.4 | 3306 | `mysqladmin ping` |
| Seeder | `backend/scripts/FilmApiSeeder/` | .NET 9 console app | N/A | Exit code (0=ok, 1=fail) |

---

## 2. Environment Variables — Docker Mapping

### 2.1 Database Connection

| .env.example var | Docker value | Reason |
|---|---|---|
| `DB_HOST=localhost` | `DB_HOST=db` | Docker service name replaces localhost |
| `DB_PORT=3306` | `DB_PORT=3306` | Unchanged |
| `DB_NAME=film-api-db` | `DB_NAME=film-api-db` | Match `MARIADB_DATABASE` |
| `DB_USER=root` | `DB_USER=root` | Unchanged |
| `DB_PASSWORD=root` | `DB_PASSWORD=Dev@12345` | Match `MARIADB_ROOT_PASSWORD` |
| `DB_USE_AUTODETECT=true` | `DB_USE_AUTODETECT=true` | Unchanged |
| `DB_SERVER_VERSION=10.11.0-mariadb` | `DB_SERVER_VERSION=10.11.0-mariadb` | Unchanged |

### 2.2 MariaDB Container

| Var | Docker value | Notes |
|---|---|---|
| `MARIADB_ROOT_PASSWORD` | `Dev@12345` | Must match DB_PASSWORD |
| `MARIADB_DATABASE` | `film-api-db` | Must match DB_NAME |

### 2.3 ASP.NET Core

| Var | Docker value |
|---|---|
| `ASPNETCORE_URLS` | `http://+:5000` (binds all interfaces in container) |

### 2.4 All Other Vars

All vars from `backend/.env.example` mapped 1:1 in `.env.docker`:
- JWT_* (JWT_SECRET, JWT_ISSUER, JWT_AUDIENCE, etc.)
- ADMIN_SEED_EMAIL, ADMIN_SEED_PASSWORD
- STRIPE_* (secret, webhook, publishable)
- SMTP_* (host, port, user, password, from_email, from_name)
- Ticket config (DEFAULT_TICKET_PRICE, HOLD_TTL_MINUTES, MAX_SEATS_PER_ORDER)
- URLs (FRONTEND_BASE_URL, TICKET_VALIDATION_BASE_URL) — keep localhost:5001 for browser redirect
- Account security (PASSWORD_RESET_TOKEN_TTL_MINUTES, SET_PASSWORD_TOKEN_TTL_MINUTES, etc.)
- OAuth: GOOGLE_OAUTH_*, MICROSOFT_OAUTH_*, MICROSOFT_TENANT_ID, MICROSOFT_ALLOWED_*
- OAuth redirect URIs: keep `http://localhost:5000/...` (browser-side redirect)
- TMDB_BEARER_TOKEN

---

## 3. Codebase Analysis

### 3.1 Backend (FilmAPI/Program.cs)

**Env loading:** Uses `DotNetEnv` package, searches for `.env` in 3 locations:
```csharp
var envCandidates = new[] {
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".env")),
    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "backend", ".env")),
    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".env"))
};
```
**Docker implication:** Paths 1 and 2 won't work in container. Need to ensure `.env` is at path 3 (`/app/.env` in container). **SOLUTION:** Copy `.env.docker` → `.env` in Dockerfile or pass envs via docker-compose `environment:` section.

**DB connection:** Constructed from `Environment.GetEnvironmentVariable("DB_HOST")` etc. Works correctly with `DB_HOST=db`.

**Health endpoint:** Checked for `/health` — Plan assumes it exists but needs verification. Can add minimal health endpoint if missing.

**Migrations:** Searched for `Database.Migrate()` — **NEEDS VERIFICATION** in Program.cs (line ~200+). If not present, must add `dbContext.Database.Migrate()` in startup.

### 3.2 Seeder (FilmApiSeeder/Program.cs)

**DB connection:** Reads same env vars (`DB_HOST`, `DB_PORT`, etc.). Uses `localhost` as default. If `DB_HOST=db` is set in env, connects correctly.

**Retry logic:** **NOT FOUND** — seeder will fail if DB not ready. Need to either:
- Add retry loop in seeder code, OR
- Use `depends_on: db (condition: service_healthy)` in docker-compose + `restart: on-failure` for retries

**Migrations:** Calls `dbContext.Database.MigrateAsync()` — good.

### 3.3 Frontend (CineBase.Web/Program.cs)

**Minimal:** Only 10 lines. Serves static files from wwwroot/.
```
app.UseStaticFiles();
app.UseDefaultFiles();
app.MapGet("/", () => Results.Redirect("/index.html"));
```
**No Node/Tailwind:** Pure .NET project, no package.json. Plan already adapted to use .NET multi-stage Dockerfile.

**Hardcoded URLs:** Check wwwroot JS files for hardcoded backend API URLs. If any reference `localhost:5000`, needs to be made configurable.

---

## 4. Issues Found

| Issue | Severity | Fix Required |
|---|---|---|
| Backend `.env` loading paths won't work in container | Critical | Copy `.env` to container or use docker-compose `environment:` |
| Seeder has no retry logic for DB readiness | High | Add retry loop or use compose restart policy |
| Backend health endpoint existence not confirmed | High | Verify /health endpoint exists; add if missing |
| Frontend may have hardcoded localhost:5000 API URLs | Medium | Search wwwroot/js/ for hardcoded URLs |
| EF Core Migrate() not confirmed in backend startup | Critical | Add `dbContext.Database.Migrate()` in Program.cs startup |

---

## 5. Health Check Endpoints

| Service | Endpoint | Method | Expected |
|---|---|---|---|
| Backend | `/health` | GET | 200 OK |
| Frontend | `/` | GET | 200 (redirects to /index.html) |
| Database | `mysqladmin ping -h localhost` | CMD | Exit 0 |

**Note:** If `/health` endpoint doesn't exist, create a minimal `MapGet("/health", () => Results.Ok("Healthy"))` in Program.cs.

---

## 6. Deliverables Status

| Deliverable | Status | Path |
|---|---|---|
| `.env.docker` | ✅ Created | Repo root (70 lines, all vars) |
| `.gitignore` updated | ✅ Done | Added `.env.docker` entry |
| Findings notepad | ✅ This file | `.sisyphus/notepads/iter6-fase0/findings.md` |

---

## 7. Next Phase Prerequisites (Phase 1: Backend Dockerfile)

Before creating backend Dockerfile:
1. ✅ Confirm DB connection string uses env vars (not hardcoded)
2. 🔲 Add `Database.Migrate()` in Program.cs startup if missing
3. 🔲 Add `/health` endpoint if missing
4. 🔲 Ensure `.env` loading works in container (path #3: `/app/.env`)

**Adaptation for Dockerfile:**
- COPY `.env.docker` → `.env` in WORKDIR (or rely on docker-compose `environment:` section)
- Backend multi-stage: build with SDK 9.0, runtime with aspnet:9.0

---

## 8. FASE 1 updates

- `backend/Dockerfile` creato con build stage SDK 9.0 e runtime stage aspnet:9.0, con caching csproj→restore e HEALTHCHECK su `/health`.
- `backend/FilmAPI/Program.cs` ora espone `GET /health` prima di `app.Run()` con risposta 200 JSON semplice.
- `frontend/CineBase.Web/wwwroot/js/pages/films.js` ora usa `window.API_BASE_URL || 'http://localhost:5000'` per le immagini da `/media/`.
