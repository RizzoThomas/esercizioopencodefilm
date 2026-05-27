# Piano di Lavoro - Iterazione 6: Containerizzazione e Deployment Azure

**Autore:** OpenCode

**Documento operativo per la containerizzazione completa di CineBase e deployment su Azure Container Apps (ACA) dopo il completamento dell'Iterazione 5.**

**Branch target suggerito:** `dev_iteration_6`

**Data Creazione:** 2026-05-26

---

## Stato Avanzamento Fasi

| Fase | Stato | Data | Note |
| --- | --- | --- | --- |
| FASE 0 - Analisi infrastruttura attuale e prerequisiti Docker | **Pianificata** | - | Inventario MariaDB existente, backend/frontend structure, env variables, FilmApiSeeder |
| FASE 1 - Dockerfile multistage backend (API .NET) | **Pianificata** | - | Build stage separato da runtime, ottimizzazione layer cache, esposizione porta 5000 |
| FASE 2 - Dockerfile multistage frontend (Tailwind + static assets) | **Pianificata** | - | Compilazione Tailwind build, ottimizzazione assets, static server su porta 5001 |
| FASE 3 - Dockerfile FilmApiSeeder con dipendenze build | **Pianificata** | - | Console app build, condivisione .env, exit code coerente con docker-compose orchestration |
| FASE 4 - Struttura docker-compose locale con database fresh | **Pianificata** | - | Services backend, frontend, db, seeder; volumes persistenti, network privata, init scripts DB |
| FASE 5 - Configurazione runtime docker-compose: env, variabili, secrets | **Pianificata** | - | .env.docker file, health checks, dependency order, retry logic, secrets management safe |
| FASE 6 - Orchestrazione startup sequence: db wait, migrations, seeding | **Pianificata** | - | init.sh script, db connectivity retry loop, EF Core migrations auto, FilmApiSeeder trigger |
| FASE 7 - Testing locale docker-compose: health, connectivity, smoke tests | **Pianificata** | - | Verifica end-to-end, login/checkout flow, PDF generation, email mock, backend API health |
| FASE 8 - Azure infrastructure: ACR, ACA environment, Azure Files | **Pianificata** | - | Container Registry setup, ACA environment with Log Analytics, Azure Files for DB + DataProtection |
| FASE 9 - Deployment database MariaDB su ACA | **Pianificata** | - | Container MariaDB su Azure Files, segreti, configurazione scaling zero, init DB script |
| FASE 10 - Deployment backend su ACA con configurazione secrets/env | **Pianificata** | - | Image push ACR, container app backend, Data Protection keys sharing, session affinity |
| FASE 11 - Deployment frontend su ACA con FQDN e domain personalizzato | **Pianificata** | - | Static frontend container, FQDN setup, custom domain optional, CORS backend alignment |
| FASE 12 - Test e smoke verification ACA end-to-end | **Pianificata** | - | Health checks, login/flow, email, PDF generation, performance baseline |
| FASE 13 - Documentazione deployment, troubleshooting e runbook | **Pianificata** | - | `.env.example` per dev locale, `.env.azure` per production, guide setup ACA, script deploy |

---

## 1) Obiettivo Iterazione

L'Iterazione 6 consolida l'architettura applicativa di CineBase introducendo **containerizzazione completa** e **deployment enterprise-ready** su infrastruttura cloud Azure secondo le best practices:

### 1.1 Containerizzazione Locale

- Tutti i servizi (backend, frontend, database, seeder) eseguiti in container gestiti da **docker-compose**
- Scenario **from-scratch**: utente clona il repository e esegue `docker-compose up -d` senza prerequisiti locali
- Database fresh da schema, auto-configured con admin account e film seeded
- Autenticazione esterna (Google, Microsoft) pre-configurata via env variables
- Email SMTP (mock locale, reale in production)
- Tutti i servizi raggiungibili su localhost con porte predefinite (API 5000, Frontend 5001, DB 3306)

### 1.2 Deployment su Azure Container Apps (ACA)

- Architettura cloud-native: zero infrastructure management
- MariaDB con storage persistente su **Azure Files**
- Backend/Frontend scalabili orizzontalmente con **session affinity** abilitata
- **Data Protection Keys** condivise tra istanze via Azure Files
- Secrets gestiti tramite **Azure Key Vault** integrato
- Dominio personalizzato con **certificato TLS/SSL gestito**
- Logging centralizzato tramite **Log Analytics**
- **Health checks** e **auto-healing** per alta disponibilità

### 1.3 Scope dell'Iterazione

**In scope:**

- Build multistage Docker per backend .NET, frontend static, seeder console
- docker-compose con orchestrazione startup sequence (db wait → migrations → seeding)
- Configurazione environment parametrizzata via `.env` con fallback sensibli
- Tests automatici locale + smoke tests in container
- Deployment infrastruttura Azure (ACR, ACA Environment, Azure Files, secrets)
- Setup MariaDB in ACA con init DB e seeder trigger
- Backend e Frontend distribuiti in ACA con configurazione production-ready
- Runbook deployment automatizzato con script Azure CLI
- Documentazione troubleshooting e best practices

**Out of scope per questa iterazione:**

- Ingress Kubernetes avanzato (ACA gestisce automaticamente)
- Auto-scaling basato su custom metrics (ACA supporta CPU/Memory, sono sufficienti)
- CI/CD GitHub Actions (precondizione per next iterations)
- Disaster recovery cross-region e backup automated (argomento futures)
- API Gateway o WAF davanti a ACA (valutabile post-MVP)
- Monitoring/observability esteso (solo logging base Log Analytics)

### 1.4 Vincoli Architetturali

1. **Docker Compose locale deve essere identity-equivalent a ACA production:** Same images, same env vars strategy, same secrets pattern
2. **No hardcoded secrets:** Tutti i segreti da env, secretref in ACA, segreto in .env locale (gitignored)
3. **Database fresh ogni startup docker-compose:** Non riutilizzare istanza preesistente; simulare scenario from-scratch
4. **Health checks su tutti i servizi:** DB, API, Frontend veramente reachable, non solo "container running"
5. **No breaking changes a Iterazione 5:** Tutti gli endpoint auth/user/admin funzionano identici in container
6. **Migrations run automaticamente su startup:** EF Core migrations applicate prima di seeding
7. **Seeder idempotente:** Può essere rieseguito senza duplicare dati (upsert, unique constraints)
8. **Frontend statico servito da HTTP server leggero** (node:alpine con serve package, oppure nginx)
9. **Base images ufficiali e updated:** mariadb:11.4+, mcr.microsoft.com/dotnet/*, node:20-alpine
10. **Logging stdout/stderr:** No file logging locale, docker gestisce centralmente

---

## 2) Requisiti Funzionali Consolidati

### 2.1 Docker Images

**Backend (FilmAPI)**

- Multi-stage build: `build` stage con SDK .NET 9, `runtime` stage con runtime .NET 9 slim
- Layer caching ottimizzato: copy csproj prima di dotnet restore
- Espone porta `5000` (default ASPNETCORE_URLS=http://+:5000)
- Healthcheck: `GET /health` oppure `GET /auth/me` che restituisca 401 se DB down, timeout 10s
- Entrypoint: `dotnet FilmAPI.dll`, args passabili con `command` in docker-compose
- Labels: `org.opencontainers.image.title=cinebase-backend`, `version=iterazione-6`

**Frontend (CineBase.Web)**

- Multi-stage: `build` stage con node:20-alpine, dotnet SDK per generare wwwroot+Tailwind
- Tailwind build incluso nel build stage (npm install → tailwindcss build → wwwroot finalized)
- `runtime` stage con server statico leggero: node:20-alpine + serve package, oppure nginx minimal
- Espone porta `5001` (configurabile via env FRONTEND_PORT)
- Healthcheck: `GET /` restituisca 200, oppure check file index.html sulla mount path
- Entrypoint: `serve -s wwwroot -l 5001` per Node, oppure `nginx` per Nginx
- Labels uguali a backend

**FilmApiSeeder (console app)**

- Multistage: `build` stage con SDK, `runtime` stage con runtime slim
- Copia `backend/.env` o riceve env vars da docker-compose
- Entrypoint: `dotnet FilmApiSeeder.dll`
- Exit code: 0 se seeding OK, 1 se errore DB connection e retry exhausted, 2 se dati inconsistenti
- Output logging stdout: JSON structured log per parsing docker logs
- Eseguito una sola volta da docker-compose tramite `docker-compose run --rm seeder`, oppure service con `restart: no` in compose
- Non necessita di esposizione porta: connessione interna a db su hostname `db:3306`

**MariaDB**

- Immagine ufficiale: `mariadb:11.4` (latest stable)
- Volumi: `/var/lib/mysql` su mount named volume `mariadb-data` (docker-compose)
- Variabili env: `MYSQL_ROOT_PASSWORD`, `MYSQL_DATABASE`, `MYSQL_INITDB_SKIP_TZINFO` (if needed)
- Init script: `/docker-entrypoint-initdb.d/init.sql` (copia schema base if needed; migrations EF allo startup backend)
- Espone porta `3306` solo su network interno `cinebase` in docker-compose
- Healthcheck: `mysqladmin ping -h localhost` con retry 30s

### 2.2 docker-compose orchestration

**Struttura base**

```yaml
version: '3.8'

services:
  db:
    image: mariadb:11.4
    container_name: cinebase-db
    networks:
      - cinebase
    environment:
      MYSQL_ROOT_PASSWORD: ${MARIADB_ROOT_PASSWORD}
      MYSQL_DATABASE: ${MARIADB_DATABASE:-cinebase}
    volumes:
      - mariadb-data:/var/lib/mysql
    ports:
      - "3306:3306"
    healthcheck:
      test: ["CMD", "mysqladmin", "ping", "-h", "localhost"]
      interval: 10s
      timeout: 5s
      retries: 30

  backend:
    image: ${BACKEND_IMAGE}
    container_name: cinebase-backend
    depends_on:
      db:
        condition: service_healthy
    networks:
      - cinebase
    environment:
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Development}
      ASPNETCORE_URLS: http://+:5000
      ConnectionStrings__FilmDbContext: Server=db;Port=3306;Database=${MARIADB_DATABASE:-cinebase};Uid=root;Pwd=${MARIADB_ROOT_PASSWORD};...
      # auth, email, stripe, etc. from .env
    ports:
      - "5000:5000"
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 10s
      timeout: 5s
      retries: 10
    restart: unless-stopped

  frontend:
    image: ${FRONTEND_IMAGE}
    container_name: cinebase-frontend
    networks:
      - cinebase
    environment:
      FRONTEND_PORT: 5001
      BACKEND_API_URL: http://localhost:5000  # per Blazor/JS fetch calls se necessario
    ports:
      - "5001:5001"
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5001/"]
      interval: 10s
      timeout: 5s
      retries: 10
    restart: unless-stopped

  seeder:
    image: ${SEEDER_IMAGE}
    container_name: cinebase-seeder
    depends_on:
      backend:
        condition: service_healthy
    networks:
      - cinebase
    environment:
      # same DB connection + TMDB key from .env
    restart: no
    # eseguito manualmente via docker-compose run --rm seeder
    # oppure orchestrato come part di backend startup

networks:
  cinebase:
    driver: bridge

volumes:
  mariadb-data:
    driver: local
```

**Variabili ambiente .env**

```env
# database
MARIADB_ROOT_PASSWORD=Dev@12345
MARIADB_DATABASE=cinebase

# aspnet
ASPNETCORE_ENVIRONMENT=Development

# backend
BACKEND_IMAGE=cinebase-backend:latest
BACKEND_PORT=5000

# frontend
FRONTEND_IMAGE=cinebase-frontend:latest
FRONTEND_PORT=5001

# seeder
SEEDER_IMAGE=cinebase-seeder:latest
TMDB_API_KEY=<your_tmdb_key>

# auth
GOOGLE_OAUTH_CLIENT_ID=<dev_google_id>
GOOGLE_OAUTH_CLIENT_SECRET=<dev_google_secret>
MICROSOFT_OAUTH_CLIENT_ID=<dev_ms_id>
MICROSOFT_OAUTH_CLIENT_SECRET=<dev_ms_secret>

# email
SMTP_SERVER=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=<dev_email>
SMTP_PASSWORD=<app_password>

# stripe
STRIPE_PUBLIC_KEY=pk_test_...
STRIPE_SECRET_KEY=sk_test_...
```

### 2.3 Sequence startup

1. **docker-compose up -d** (da root repo)
2. **db service avvia**: maria DB, legge env, crea schema base, healthcheck loop
3. **backend service avvia** dopo che db health OK: .NET startup, EF Core migrations auto-apply, warmup di 10s
4. **frontend service avvia** dopo backend health OK: static server ascolta 5001
5. **seeder service** (opzionale, run-once): eseguito manualmente o triggered da script `docker-compose-init.sh`
   - connette a backend API su http://backend:5000
   - verifica DB state, upsert film/cinema/show
   - genera mock user admin se non exists
6. **all services healthy** → pronto per dev/test

### 2.4 Configurazione Azure Infrastructure

**Azure Container Registry (ACR)**

- Resource group: `cinebase-rg`
- Location: `italynorth` (o nearest Azure region)
- SKU: `Basic` (sufficient for MVP, ~$30/month)
- Image repositories:
  - `cinebase-backend:latest`
  - `cinebase-frontend:latest`
  - `cinebase-seeder:latest`

**Azure Files (Storage Account)**

- Account name: `cinebasefiles<unique>` (storageV2, Standard_LRS)
- File shares:
  - `mariadb-data` (quota 5 GB): /var/lib/mysql MariaDB files
  - `webapp-dataprotection` (quota 1 GB): ASP.NET Core Data Protection Keys per session affinity + scaling
- Access: shared connection string salvato come secret ACA

**Azure Container Apps Environment (ACA)**

- Name: `cinebase-env`
- Location: `italynorth`
- Log Analytics workspace: `cinebase-logs`
  - Retention: 30 days (default)
  - Used for debugging + monitoring

**ACA Services (Container Apps)**

1. **MariaDB Container App**
   - Name: `cinebase-db`
   - Image: `mariadb:11.4`
   - Resources: CPU 0.5, Memory 1Gi
   - Ingress: internal (no public access)
   - Replicas: min 0, max 1 (cost savings; cold start ~30s acceptable per demo)
   - Mount point: `/var/lib/mysql` → Azure Files share `mariadb-data`
   - Env: MYSQL_ROOT_PASSWORD=secretref:mariadb-password (segreto ACA), MYSQL_DATABASE=cinebase
   - Healthcheck: mysqladmin ping

2. **Backend Container App**
   - Name: `cinebase-api`
   - Image: `$ACR_LOGIN_SERVER/cinebase-backend:latest`
   - Resources: CPU 0.5, Memory 1Gi
   - Replicas: min 0, max 3 (auto-scale on CPU > 70%)
   - Ingress: external, port 5000 (pubblica su `https://cinebase-api.<generated-domain>.azurecontainerapps.io`)
   - Mount point: `/mnt/dataprotection` → Azure Files share `webapp-dataprotection`
   - Env vars: ConnectionString con db URL internal (cinebase-db.internal.azurecontainerapps.io), tutti gli altri da secretref
   - Session affinity: enabled
   - Health endpoint: `/health` port 5000

3. **Frontend Container App**
   - Name: `cinebase-web`
   - Image: `$ACR_LOGIN_SERVER/cinebase-frontend:latest`
   - Resources: CPU 0.5, Memory 512Mi (static serve needs less)
   - Replicas: min 1, max 3 (CDN + static cache, min 1 per HA baseline)
   - Ingress: external, port 5001 → custom domain se disponibile
   - Env: BACKEND_API_URL=https://cinebase-api.<internal-domain>
   - Health endpoint: `/` port 5001
   - CORS headers configured lato backend per frontend public URL

---

## 3) Decisioni Architetturali

### 3.1 Perché Multi-stage Dockerfile

**Problema:** Base images con SDK (.NET SDK, node full) sono pesanti (~2 GB compresse). Runtime images sono ~100-200 MB.

**Soluzione:** Separare build stage (con SDK) da runtime stage (solo runtime), copiando gli artifact finali.

**Beneficio:**
- Immagini finali ~5x più piccole → pull/start faster
- Layer cache migliore: SDK image pulled raro, runtime sempre cached
- Isolamento build tools da runtime production
- Zero sorgenti in immagine finale

### 3.2 Perché docker-compose locale deve mirrare ACA

**Problema:** "Works on my machine" syndrome; sviluppatore testa localmente con setup diverso (db su host, backend su IDE), deployment fallisce.

**Soluzione:** docker-compose **must be** identity-equivalent a ACA:
- Same images (Dockerfile identity + tag)
- Same env var naming/strategy
- Same secrets pattern (envfile secrets in compose, secretref in ACA)
- Same startup sequence + health checks
- Same port mappings and networking

**Beneficio:**
- High confidence che container da dev porta via a prod senza sorprese
- Integration testing "for real" prima di push
- Team align on production-like environment

### 3.3 Perché database fresh ogni docker-compose up

**Problema:** Riutilizzare volume DB locale mantiene stato residuo da run precedente; seed non è idempotent, genera duplicati; hard debug quali film/data sono veri vs. vecchie run.

**Soluzione:** Valutare **two strategies**:

**Option A (Recommended - Clean State):**
- docker-compose down (ferma containers, **rimuove volumes** con `docker-compose down -v`)
- docker-compose up (crea volumi fresh, database auto-initialized dal container maria, migrations auto-run da backend)
- docker-compose-init.sh trigger seeder una volta che backend healthy

**Option B (Dev Ergonomics):**
- docker-compose.dev.yml con named volume **persisted** (non rimosso da down)
- db.volumes: mariadb-dev-persist:/var/lib/mysql (opzionale, default fresh)
- Frontend auto-reload su file change (volume mount wwwroot)
- But ensure seeder is truly idempotent (upsert all film/show, don't re-insert)

**Choice for Iteration 6:** Start con Option A (clean state ogni up). Se developer experience degraded, pivot a Option B + idempotency hardening in seeder.

### 3.4 Perché Azure Files per Data Protection Keys

**Problema:** ASP.NET Core per default salva Data Protection Keys in `~/.aspnet/DataProtection-Keys/` (OS-specific, per user). In container:
- Ogni container ha FS effimero per /root
- Scaling a 2+ istanze backend: ogni istanza ha **different** keys
- Sessione user creata da istanza 1 non decrittata da istanza 2
- Result: login fails randomicamente con "invalid session token"

**Soluzione:** Usare **Azure Files shared mount** per `/mnt/dataprotection`:
- Tutte le istanze backend leggono/scrivono **same** keys
- ASP.NET Core config in Program.cs: `builder.Services.AddDataProtection().SetApplicationName("CineBase").PersistKeysToFileSystem(new DirectoryInfo("/mnt/dataprotection"))`
- Istanza 2 decritta token dell'istanza 1 correctly
- Session affinity abilitata in ACA per ottimizzazione (redirect utente alla stessa istanza se possible)

**Trade-off:** Minimal latency aggiunto (Azure Files IOPS ok per auth key operations). Alternative più costose: Azure Key Vault (overhead management), Redis (extra service).

### 3.5 MariaDB scaling zero in ACA

**Decision:** `min-replicas 0` per MariaDB container app.

**Rationale:**
- Demo/development cost minimization: ~$10/month → ~$0 quando scaled a zero
- DB cold start acceptable (~30s): Dev/test scenario, non production time-critical
- Production post-MVP: Pivot a min-replicas 1 con dedicated reserved capacity

**Trade-off:** Prima richiesta post-scale-down avrà latenza. Backend deve tollerare transient failure. Implementare retry logic (HttpClient policy in backend, Polly library).

### 3.6 Session Affinity abilitata su backend ACA

**Decision:** Enable session affinity in ACA backend container app.

**Why:**
- Anche con Data Protection Keys condivise, affinità riduce network round-trips per decrypt keys
- Stato in memoria di utente (DTO cache, temp var) mantiene consistency
- Bilanciator round-robin ACA non supporta custom strategy, ma affinità è built-in con cookie sticky

**Implementation:** ACA parameter `--enable-session-affinity` quando creare backend app.

### 3.7 Frontend statico: Node serve vs. Nginx

**Option A (Node + serve):** Base image node:20-alpine, `npm install serve` in build, entrypoint `serve -s wwwroot -l 5001`
- Pros: Single language runtime (JS suite), easy to debug, small image
- Cons: Node overhead for pure static serving

**Option B (Nginx):** Base image nginx:alpine, COPY wwwroot config
- Pros: Industry standard for static, tiny image, optimal performance
- Cons: Nginx config complexity, extra language

**Choice for Iteration 6:** Start con **Option A (Node serve)** per consistency (tutto gira su container leggeri, no ops overhead Nginx). Post-MVP: Switch a Nginx se static throughput critical.

---

## 4) Design Tecnico - Dockerfile e Compose

### 4.1 Backend Dockerfile (FilmAPI)

File: `backend/Dockerfile`

```dockerfile
# Multi-stage build

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy solution and projects
COPY ["backend/FilmAPI/FilmAPI.csproj", "backend/FilmAPI/"]
COPY ["tests/backend/FilmAPI.Tests.csproj", "tests/backend/"]

# Restore
RUN cd backend/FilmAPI && dotnet restore

# Copy entire source
COPY backend/FilmAPI/ backend/FilmAPI/

# Build
RUN cd backend/FilmAPI && dotnet build -c Release -o /app/build

# Publish
RUN cd backend/FilmAPI && dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy from build stage
COPY --from=build /app/publish .

# Metadata
LABEL org.opencontainers.image.title="CineBase Backend"
LABEL org.opencontainers.image.version="iteration-6"

# Health check
HEALTHCHECK --interval=10s --timeout=5s --retries=10 \
    CMD curl -f http://localhost:5000/health || exit 1

# Entrypoint
ENTRYPOINT ["dotnet", "FilmAPI.dll"]
```

### 4.2 Frontend Dockerfile (CineBase.Web)

File: `frontend/Dockerfile`

**NOTE (Adaptation from validation):** CineBase.Web is a pure ASP.NET static file server (no Node/Tailwind build). Use .NET multi-stage build instead.

```dockerfile
# Multi-stage: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy project file
COPY ["frontend/CineBase.Web/CineBase.Web.csproj", "frontend/CineBase.Web/"]

# Restore dependencies
RUN cd frontend/CineBase.Web && dotnet restore

# Copy all source code
COPY frontend/CineBase.Web/ frontend/CineBase.Web/

# Build
RUN cd frontend/CineBase.Web && dotnet build -c Release -o /app/build

# Publish
RUN cd frontend/CineBase.Web && dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy published artifacts from build stage
COPY --from=build /app/publish .

# Metadata
LABEL org.opencontainers.image.title="CineBase Frontend"
LABEL org.opencontainers.image.version="iteration-6"

# Health check
HEALTHCHECK --interval=10s --timeout=5s --retries=10 \
    CMD curl -f http://localhost:5001/ || exit 1

# Entrypoint - serves static content from wwwroot
ENTRYPOINT ["dotnet", "CineBase.Web.dll"]
```

### 4.3 Seeder Dockerfile

File: `backend/scripts/FilmApiSeeder/Dockerfile`

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

COPY ["backend/scripts/FilmApiSeeder/FilmApiSeeder.csproj", "."]
RUN dotnet restore

COPY backend/scripts/FilmApiSeeder/ .
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app

COPY --from=build /app/publish .

# Metadata
LABEL org.opencontainers.image.title="CineBase Film API Seeder"
LABEL org.opencontainers.image.version="iteration-6"

ENTRYPOINT ["dotnet", "FilmApiSeeder.dll"]
```

### 4.4 docker-compose.yml (locale)

File: `docker-compose.yml` (at repo root)

```yaml
version: '3.8'

services:
  db:
    image: mariadb:11.4
    container_name: cinebase-db
    environment:
      MYSQL_ROOT_PASSWORD: ${MARIADB_ROOT_PASSWORD:-Dev@12345}
      MYSQL_DATABASE: ${MARIADB_DATABASE:-cinebase}
      MYSQL_INITDB_SKIP_TZINFO: 'yes'
    volumes:
      - mariadb-data:/var/lib/mysql
    ports:
      - "3306:3306"
    networks:
      - cinebase
    healthcheck:
      test: ["CMD", "mysqladmin", "ping", "-h", "localhost"]
      interval: 10s
      timeout: 5s
      retries: 30
      start_period: 5s

  backend:
    build:
      context: .
      dockerfile: backend/Dockerfile
    container_name: cinebase-backend
    image: cinebase-backend:latest
    depends_on:
      db:
        condition: service_healthy
    environment:
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Development}
      ASPNETCORE_URLS: http://+:5000
      ConnectionStrings__FilmDbContext: "Server=db;Port=3306;Database=${MARIADB_DATABASE:-cinebase};Uid=root;Pwd=${MARIADB_ROOT_PASSWORD:-Dev@12345};AllowPublicKeyRetrieval=true;Pooling=true;Connection Lifetime=3600;"
      # Auth
      GOOGLE_OAUTH_CLIENT_ID: ${GOOGLE_OAUTH_CLIENT_ID}
      GOOGLE_OAUTH_CLIENT_SECRET: ${GOOGLE_OAUTH_CLIENT_SECRET}
      GOOGLE_OAUTH_REDIRECT_URI: http://localhost:5000/auth/external/google/callback
      MICROSOFT_OAUTH_CLIENT_ID: ${MICROSOFT_OAUTH_CLIENT_ID}
      MICROSOFT_OAUTH_CLIENT_SECRET: ${MICROSOFT_OAUTH_CLIENT_SECRET}
      MICROSOFT_OAUTH_REDIRECT_URI: http://localhost:5000/auth/external/microsoft/callback
      MICROSOFT_TENANT_ID: ${MICROSOFT_TENANT_ID}
      MICROSOFT_ALLOWED_TENANT_ID: ${MICROSOFT_ALLOWED_TENANT_ID}
      MICROSOFT_ALLOWED_DOMAIN: issgreppi.it
      # Email SMTP
      SMTP_SERVER: ${SMTP_SERVER:-smtp.gmail.com}
      SMTP_PORT: ${SMTP_PORT:-587}
      SMTP_USERNAME: ${SMTP_USERNAME}
      SMTP_PASSWORD: ${SMTP_PASSWORD}
      SMTP_USE_SSL: 'false'
      SMTP_SENDER_EMAIL: ${SMTP_SENDER_EMAIL}
      SMTP_SENDER_NAME: ${SMTP_SENDER_NAME:-CineBase Support}
      # Stripe
      STRIPE_PUBLIC_KEY: ${STRIPE_PUBLIC_KEY}
      STRIPE_SECRET_KEY: ${STRIPE_SECRET_KEY}
      # TMDB
      TMDB_API_KEY: ${TMDB_API_KEY}
      # Frontend URL
      FRONTEND_BASE_URL: http://localhost:5001
      # JWT
      JWT_SECRET: ${JWT_SECRET:-dev-secret-min-32-chars-change-prod}
      JWT_ISSUER: ${JWT_ISSUER:-CineBase}
      JWT_AUDIENCE: ${JWT_AUDIENCE:-CineBase}
      JWT_EXPIRY_MINUTES: ${JWT_EXPIRY_MINUTES:-60}
    ports:
      - "5000:5000"
    volumes:
      - ./backend/FilmAPI:/app/src
    networks:
      - cinebase
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 30s
    restart: unless-stopped

  frontend:
    build:
      context: .
      dockerfile: frontend/Dockerfile
    container_name: cinebase-frontend
    image: cinebase-frontend:latest
    environment:
      FRONTEND_PORT: 5001
      BACKEND_API_URL: http://localhost:5000
    ports:
      - "5001:5001"
    volumes:
      - ./frontend/CineBase.Web/wwwroot:/app/wwwroot
    networks:
      - cinebase
    healthcheck:
      test: ["CMD", "wget", "-q", "-O-", "http://localhost:5001/"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 10s
    restart: unless-stopped
    depends_on:
      - backend

  seeder:
    build:
      context: .
      dockerfile: backend/scripts/FilmApiSeeder/Dockerfile
    image: cinebase-seeder:latest
    container_name: cinebase-seeder
    environment:
      ConnectionStrings__FilmDbContext: "Server=db;Port=3306;Database=${MARIADB_DATABASE:-cinebase};Uid=root;Pwd=${MARIADB_ROOT_PASSWORD:-Dev@12345};AllowPublicKeyRetrieval=true;"
      TMDB_API_KEY: ${TMDB_API_KEY}
      BACKEND_API_URL: http://backend:5000
      # Seeder deve popolare admin user + film
    networks:
      - cinebase
    depends_on:
      backend:
        condition: service_healthy
    restart: no
    # Eseguito manualmente o via docker-compose-init.sh

networks:
  cinebase:
    driver: bridge

volumes:
  mariadb-data:
    driver: local
```

### 4.5 .env.example (template per developer)

File: `.env.example`

```env
###############################################
# Docker Compose - Development Local Setup
###############################################

# Database
MARIADB_ROOT_PASSWORD=Dev@12345
MARIADB_DATABASE=cinebase

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Development

# Backend
BACKEND_PORT=5000

# Frontend
FRONTEND_PORT=5001

# Seeder
TMDB_API_KEY=<GET_FROM_TMDB_API_DASHBOARD>

# Authentication - Google
GOOGLE_OAUTH_CLIENT_ID=<CREATE_IN_GOOGLE_CLOUD_CONSOLE>
GOOGLE_OAUTH_CLIENT_SECRET=<CREATE_IN_GOOGLE_CLOUD_CONSOLE>

# Authentication - Microsoft
MICROSOFT_OAUTH_CLIENT_ID=<CREATE_IN_ENTRA_ID_PORTAL>
MICROSOFT_OAUTH_CLIENT_SECRET=<CREATE_IN_ENTRA_ID_PORTAL>
MICROSOFT_TENANT_ID=<YOUR_TENANT_ID>
MICROSOFT_ALLOWED_TENANT_ID=<SCHOOL_TENANT_ID>

# Email SMTP (Gmail example)
SMTP_SERVER=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=<YOUR_GMAIL_ADDRESS>
SMTP_PASSWORD=<GMAIL_APP_PASSWORD>
SMTP_SENDER_EMAIL=<YOUR_GMAIL_ADDRESS>
SMTP_SENDER_NAME=CineBase Support

# Stripe (Test keys for development)
STRIPE_PUBLIC_KEY=pk_test_<YOUR_TEST_KEY>
STRIPE_SECRET_KEY=sk_test_<YOUR_TEST_KEY>

# Frontend URL (for links in emails, auth redirects)
FRONTEND_BASE_URL=http://localhost:5001

# JWT Secrets (min 32 chars for production)
JWT_SECRET=dev-secret-min-32-chars-change-production
JWT_ISSUER=CineBase
JWT_AUDIENCE=CineBase
JWT_EXPIRY_MINUTES=60
```

### 4.6 docker-compose-init.sh (orchestration script)

File: `docker-compose-init.sh` (at repo root, chmod +x)

```bash
#!/bin/bash
set -e

echo "======================================"
echo "CineBase Docker Compose Initialization"
echo "======================================"

# Check if .env exists
if [ ! -f .env ]; then
  echo "ERROR: .env file not found. Copy from .env.example and fill in values."
  exit 1
fi

# Load .env
source .env

echo ""
echo "Step 1: Removing old containers and volumes..."
docker-compose down -v || true
echo "✓ Done"

echo ""
echo "Step 2: Building Docker images..."
docker-compose build --no-cache
echo "✓ Done"

echo ""
echo "Step 3: Starting services (db, backend, frontend)..."
docker-compose up -d db backend frontend
echo "✓ Services starting..."

echo ""
echo "Step 4: Waiting for backend health (max 60 seconds)..."
for i in {1..60}; do
  if docker-compose exec -T backend curl -f http://localhost:5000/health > /dev/null 2>&1; then
    echo "✓ Backend is healthy!"
    break
  fi
  echo "  Checking... ($i/60)"
  sleep 1
done

echo ""
echo "Step 5: Running database seeder..."
docker-compose run --rm seeder
SEEDER_EXIT=$?
if [ $SEEDER_EXIT -ne 0 ]; then
  echo "WARNING: Seeder exited with code $SEEDER_EXIT. Check logs with: docker-compose logs seeder"
  # Don't fail completely; seeding can be re-run manually
fi

echo ""
echo "======================================"
echo "✓ CineBase is ready!"
echo "======================================"
echo ""
echo "Available at:"
echo "  Frontend:  http://localhost:5001"
echo "  Backend:   http://localhost:5000"
echo "  Database:  localhost:3306 (root / $MARIADB_ROOT_PASSWORD)"
echo ""
echo "Commands:"
echo "  docker-compose logs -f backend       # Backend logs"
echo "  docker-compose logs -f frontend      # Frontend logs"
echo "  docker-compose logs -f db            # Database logs"
echo "  docker-compose exec db mysql -u root -p$MARIADB_ROOT_PASSWORD $MARIADB_DATABASE  # MySQL CLI"
echo "  docker-compose down -v               # Stop and remove all (including volumes)"
echo ""
```

---

## 5) Fasi di Implementazione

### FASE 0 - Analisi infrastruttura attuale e prerequisiti Docker

**Obiettivo**: Baseline infrastruttura, inventario config, prerequisiti per containerizzazione.

**Attività**:

1. Inventariare configurazione attuale:
   - `backend/.env.example` e variabili usate
   - `backend/FilmAPI/Program.cs` startup config
   - `frontend/CineBase.Web/wwwroot` structure
   - `backend/scripts/FilmApiSeeder` - entry point e dipendenze

2. Verificare running prerequisites:
   - Docker Desktop installed and running
   - Docker Compose available (v2+)
   - .NET 9 SDK locally (for non-container dev fallback)
   - Node 20+ (for frontend local Tailwind build if needed)

3. Creare `.env.example` template a repo root con **tutte** le variabili usate in iterazione 5 + nuove per containerizzazione

4. Documentare current database state:
   - Current schema (EF Core model)
   - Current migrations count
   - Admin user seed values
   - Default port 3306 for MariaDB

5. Verificare no hardcoded localhost/absolute paths nel codice backend/frontend:
   ```bash
   grep -r "localhost:5000" frontend/
   grep -r "C:\\Users\\" backend/FilmAPI/
   grep -r "D:/Projects" backend/
   ```

**Verifica fase**:

- `.env.example` completo e documentato
- Dockerfile skeleton files created ma vuoti
- docker-compose.yml skeleton created
- Baseline test: `dotnet test backend/` still passes

**Test automatici minimi**: N/A (analisi fase)

**Checklist fase**:

- [ ] `.env.example` creato con ALL variabili
- [ ] Prerequisiti Docker documentati in README
- [ ] Inventario current config completo
- [ ] No hardcoded path/localhost trovati
- [ ] Baseline tests verdi pre-containerizzazione

---

### FASE 1 - Dockerfile multistage backend (API .NET)

**Obiettivo**: Build multistage backend, ottimizzato cache e slim runtime.

**Attività backend**:

1. Creare `backend/Dockerfile` con:
   - Build stage: mcr.microsoft.com/dotnet/sdk:9.0
   - Restore dependencies da csproj
   - Build -c Release
   - Publish -o /app/publish

2. Runtime stage:
   - mcr.microsoft.com/dotnet/aspnet:9.0
   - Copy --from=build /app/publish .
   - Health check endpoint `/health` (GET, verify DB connection if possible)

3. Layer optimization:
   - COPY .csproj FIRST before source code (cache on dependency change only)
   - Build context: `.dockerignore` esclude `bin/`, `obj/`, `.git/`, `.vs/`

4. Labels: org.opencontainers.image.title, version

5. ENTRYPOINT: `["dotnet", "FilmAPI.dll"]`

6. Verify build locally:
   ```bash
   docker build -f backend/Dockerfile -t cinebase-backend:latest .
   docker run -it --rm -e ASPNETCORE_ENVIRONMENT=Development cinebase-backend:latest
   # Expect startup logs, then ERROR (no DB connection yet - expected)
   ```

**Verifiche**:

- Build completa senza errori
- Final image ~400-500 MB (vs. SDK image 2 GB+)
- Layer cache test: modify source file, rebuild; csproj restore skipped (cache hit)
- Entrypoint works: `docker run -it --rm ... /bin/bash` → can exec dotnet commands

**Test automatici minimi**:

- Dockerfile builds successfully
- Image runs without container exit code > 0 immediately (timeout 5s acceptable for missing DB)
- Health check endpoint responds (even 500 acceptable showing app started)

**Checklist fase**:

- [ ] `backend/Dockerfile` creato con multistage
- [ ] `.dockerignore` creato
- [ ] Layer cache optimizzato
- [ ] Labels added
- [ ] Local build test OK
- [ ] Image size <600 MB

---

### FASE 2 - Dockerfile multistage frontend (Tailwind + static assets)

**Obiettivo**: Build frontend con compilazione Tailwind, serve statico.

**Attività frontend**:

1. Creare `frontend/Dockerfile` con:
   - Build stage: node:20-alpine
   - COPY package.json + tailwind.config.js
   - npm install
   - npm run build:css (Tailwind build script in package.json)
   - COPY wwwroot + tailwind input files

2. Runtime stage:
   - node:20-alpine
   - npm install -g serve
   - COPY --from=build /app/wwwroot ./wwwroot
   - Health check: GET http://localhost:5001/ → 200

3. Environment variable FRONTEND_PORT (default 5001)

4. Labels: same as backend

5. Entrypoint: `serve -s wwwroot -l $FRONTEND_PORT`

6. .dockerignore: node_modules, .git, dist (if exists), coverage

7. Build test:
   ```bash
   docker build -f frontend/Dockerfile -t cinebase-frontend:latest .
   docker run -it --rm -p 5001:5001 cinebase-frontend:latest
   # curl http://localhost:5001/ → index.html
   ```

**Verifiche**:

- Build completa senza npm errors
- Tailwind CSS generato in wwwroot (check file size > 50 KB for CSS output)
- Serve process listens su 5001
- Index.html accessible su http://localhost:5001/
- Assets (CSS, JS) loaded correctly

**Test automatici minimi**:

- Dockerfile builds successfully
- Health check endpoint responds 200
- Static files served correctly
- No console errors on page load (check logs)

**Checklist fase**:

- [ ] `frontend/Dockerfile` creato
- [ ] npm build:css script configured in package.json
- [ ] Tailwind compiled in build stage
- [ ] Runtime image <100 MB
- [ ] Serve listening on configurable port
- [ ] Assets loading correctly

---

### FASE 3 - Dockerfile FilmApiSeeder console app

**Obiettivo**: Seeder as container, idempotent film/cinema seeding.

**Attività seeder**:

1. Creare `backend/scripts/FilmApiSeeder/Dockerfile`:
   - Build stage: SDK 9.0
   - Publish -c Release
   - Runtime stage: runtime 9.0
   - COPY published app
   - ENTRYPOINT: `dotnet FilmApiSeeder.dll`

2. Exit codes:
   - 0 = success (DB seeded, or already seeded idempotently)
   - 1 = DB connection error after retries
   - 2 = data validation error (inconsistent state)

3. Logging: stdout JSON structured log per parsing docker logs

4. FilmApiSeeder.csproj deve incluso:
   - Database context + connection string support
   - TMDB API client
   - Upsert logic (not insert-only)

5. Example FilmApiSeeder Main logic:
   ```csharp
   var dbConnection = await WaitForDatabaseAsync(connectionString, maxRetries: 10);
   if (!dbConnection) Environment.Exit(1);

   await MigrateAsync(dbConnection);
   await SeedFilmsAsync(connectionString); // upsert by TMDB ID
   await SeedCinemasAsync(connectionString); // upsert by name
   await SeedAdminUserAsync(connectionString);

   Console.WriteLine("{\"status\":\"seeding_complete\"}");
   Environment.Exit(0);
   ```

**Verifiche**:

- Build completa
- Run with test .env vars → seeder connects, migrations run, data seeded
- Re-run same seeder → idempotent, no duplicates
- Exit code 0 on success
- JSON logs parseable by docker logs

**Test automatici minimi**:

- Dockerfile builds
- Seeder connects to running MariaDB
- Seeds data idempotently
- Exit code 0
- No duplicate records on re-run

**Checklist fase**:

- [ ] `FilmApiSeeder/Dockerfile` creato
- [ ] Seeder logic idempotente (upsert)
- [ ] Exit codes correct
- [ ] JSON logging implemented
- [ ] DB migration auto-run before seed
- [ ] Admin user created if not exists

---

### FASE 4 - Struttura docker-compose locale con database fresh

**Obiettivo**: docker-compose.yml completo con tutte services, volumes, networks.

**Attività**:

1. Creare `docker-compose.yml` a repo root con:
   - db service: mariadb:11.4, volumes mariadb-data, port 3306, healthcheck
   - backend service: build ./backend/Dockerfile, depends_on db healthy, port 5000
   - frontend service: build ./frontend/Dockerfile, port 5001
   - seeder service: build ./FilmApiSeeder/Dockerfile, depends_on backend healthy, restart no
   - networks: cinebase bridge
   - volumes: mariadb-data

2. .dockerignore per backend, frontend, seeder:
   - bin/, obj/, dist/, node_modules/
   - .git/, .vs/, .vscode/
   - test coverage, IDE files

3. Verific local docker-compose:
   ```bash
   docker-compose config  # validate YAML syntax + variable interpolation
   ```

4. Test build (no run yet):
   ```bash
   docker-compose build
   ```

5. Document docker-compose structure:
   - Service dependency DAG
   - Port mappings
   - Environment variables sourced from .env
   - Health check endpoints per service

**Verifiche**:

- docker-compose.yml valid YAML
- All images build without error
- Services defined correctly
- Dependencies (depends_on) correct DAG
- Volumes named correctly
- Networks isolated

**Test automatici minimi**:

- docker-compose config exits 0
- docker-compose build exits 0
- No pull-required errors (all images available locally or docker hub)

**Checklist fase**:

- [ ] `docker-compose.yml` creato
- [ ] `.dockerignore` files creati
- [ ] Services DAG correct
- [ ] Config validato
- [ ] Build successful
- [ ] Documentation README.md updated per Docker setup

---

### FASE 5 - Configurazione runtime docker-compose: env, variabili, secrets

**Obiettivo**: `.env` file strategy, secrets safe (no git), environment variable mapping.

**Attività**:

1. Aggiornare `.env.example` con TUTTE variabili usate in docker-compose.yml

2. Creare `.env.local.example` (per secret values):
   - MARIADB_ROOT_PASSWORD=<CHANGE_ME>
   - GOOGLE_OAUTH_CLIENT_SECRET=<CHANGE_ME>
   - SMTP_PASSWORD=<CHANGE_ME>
   - Stripe keys

3. Add `.env.local` to `.gitignore` (per evitare commit accidentali secrets)

4. Docker-compose env mapping:
   - Leggere from .env file automaticamente
   - Reference syntax `${VAR_NAME}` in compose
   - Fallback default con `${VAR_NAME:-default_value}`

5. Documentare env variables per ambiente:
   - Development (docker-compose.yml)
   - Production (azure deploy scripts .env.azure)

6. Verify .env parsing:
   ```bash
   docker-compose config  # shows interpolated values
   ```

7. Secrets non hardcodati nel compose:
   - MARIADB_ROOT_PASSWORD, SMTP_PASSWORD, STRIPE_SECRET_KEY = from .env only
   - No secrets in docker-compose.yml literals

**Verifiche**:

- .env.example complete (no missing vars)
- .env.local in .gitignore
- docker-compose config shows correct var values
- No secrets logged or visible in images

**Test automatici minimi**:

- docker-compose config exits 0 with .env present
- Test .env missing → build fails gracefully with helpful message (or uses fallback defaults)

**Checklist fase**:

- [ ] `.env.example` completo
- [ ] `.env.local` in .gitignore
- [ ] Env vars documentati in README
- [ ] No hardcoded secrets in compose
- [ ] config validato
- [ ] Fallback defaults sensati

---

### FASE 6 - Orchestrazione startup sequence: db wait, migrations, seeding

**Obiettivo**: Garantire DB ready → migrations run → seeding → services healthy in sequence.

**Attività**:

1. docker-compose.yml depends_on:
   - backend depends_on: db (condition: service_healthy)
   - frontend depends_on: backend (condition: service_healthy)
   - seeder depends_on: backend (condition: service_healthy)

2. Health checks per service:
   - db: mysqladmin ping
   - backend: curl /health (verifica DB connectible, not just app running)
   - frontend: wget / oppure curl /index.html

3. Backend startup (Program.cs):
   - EF Core migrations auto-run on startup
   - Add to Program.cs:
     ```csharp
     using (var scope = app.Services.CreateScope())
     {
       var dbContext = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
       dbContext.Database.Migrate();
       Logger.Info("Migrations applied");
     }
     ```

4. Docker-compose-init.sh script (bash):
   - docker-compose down -v (clean volumes)
   - docker-compose up -d db
   - Poll db health (loop 30s, retry every 1s)
   - docker-compose up -d backend frontend
   - Poll backend health (loop 60s)
   - docker-compose run --rm seeder
   - Final health check all services

5. Test startup sequence locally:
   ```bash
   bash docker-compose-init.sh
   ```

**Verifiche**:

- db startup: ~5s
- backend startup: ~10s (migrations run time-dependent on count, usually <5s)
- frontend startup: ~3s
- Total: ~20-30s for ready state
- Seeder run: ~30s (depends on TMDB API calls)

**Test automatici minimi**:

- docker-compose up sequence respects depends_on
- Health checks actually verify connectivity (not just container running)
- Migrations applied before backend routes available
- Seeder runs after backend healthy

**Checklist fase**:

- [ ] depends_on with condition: service_healthy configured
- [ ] Health checks verifies actual service readiness
- [ ] EF migrations auto-run in Program.cs
- [ ] docker-compose-init.sh script funzionante
- [ ] Startup sequence <45s total
- [ ] All services healthy post-startup

---

### FASE 7 - Testing locale docker-compose: health, connectivity, smoke tests

**Obiettivo**: Verificare end-to-end container stack locally before pushing to Azure.

**Attività**:

1. Smoke test suite bash script `docker-compose-smoke-test.sh`:
   - GET /health → 200
   - GET /auth/me → 401 (unauthenticated)
   - GET / (frontend) → 200 + HTML content
   - POST /auth/register (create test user) → 201
   - POST /auth/login (test user) → 200 + token
   - GET /auth/me (authenticated) → 200 + user data
   - Database query via mysql CLI → count users > 0
   - Check docker logs for errors

2. Integration test suite xUnit:
   - Keep existing backend tests
   - Add container-specific tests (if needed):
     - Database connection string from env
     - API base URL from container network (http://backend:5000 if running in docker)
   - Run tests inside container or against running container

3. Frontend smoke test:
   - curl http://localhost:5001/ → 200 + index.html present
   - wget http://localhost:5001/css/... → static assets load
   - Check for console errors (run in headless browser if Playwright available)

4. Email test (SMTP mock):
   - Trigger password reset flow
   - Verify email logged in mock SMTP (if using smtp4dev or similar)
   - Or check backend logs for email send attempt

5. PDF generation test:
   - Trigger ticket generation flow
   - Verify PDF generated and downloadable

6. Cleanup test:
   - docker-compose down -v
   - Verify no orphaned processes or volumes
   - Re-run docker-compose up → clean state again

**Verifiche**:

- All smoke test endpoints return expected status codes
- JSON responses valid
- No 5xx errors in backend logs
- Frontend loads and renders
- Assets served correctly
- Database accessible from all services

**Test automatici minimi**:

- Run docker-compose-smoke-test.sh → all checks pass
- Backend integration tests pass against running container
- Frontend loads without 404s

**Checklist fase**:

- [ ] `docker-compose-smoke-test.sh` script created
- [ ] Smoke tests tutte verdi
- [ ] Backend integration tests pass
- [ ] Frontend loads + assets OK
- [ ] No 5xx errors in logs
- [ ] Email/PDF flows work
- [ ] Clean down + restart successful

---

### FASE 8 - Azure infrastructure: ACR, ACA environment, Azure Files

**Obiettivo**: Creare Azure resources prerequisite per deployment.

**Attività**:

1. **Azure subscription setup:**
   - Verify user has Contributor role in subscription
   - `az login && az account show`

2. **Create Azure Container Registry (ACR):**
   ```bash
   RESOURCE_GROUP="cinebase-rg"
   LOCATION="italynorth"
   ACR_NAME="cinebase$(date +%s | tail -c 4)"
   
   az group create --name $RESOURCE_GROUP --location $LOCATION
   az acr create --resource-group $RESOURCE_GROUP --name $ACR_NAME --sku Basic --admin-enabled true
   ACR_LOGIN_SERVER=$(az acr show --name $ACR_NAME --query loginServer --output tsv)
   ```

3. **Create Log Analytics Workspace + ACA Environment:**
   ```bash
   LOG_WORKSPACE="cinebase-logs-$(date +%s | tail -c 4)"
   ACA_ENV="cinebase-env"
   
   az monitor log-analytics workspace create --resource-group $RESOURCE_GROUP --workspace-name $LOG_WORKSPACE --location $LOCATION
   LOG_WS_ID=$(az monitor log-analytics workspace show --query customerId -g $RESOURCE_GROUP -n $LOG_WORKSPACE --out tsv)
   LOG_WS_KEY=$(az monitor log-analytics workspace get-shared-keys --query primarySharedKey -g $RESOURCE_GROUP -n $LOG_WORKSPACE --out tsv)
   
   az containerapp env create --name $ACA_ENV --resource-group $RESOURCE_GROUP --location $LOCATION --logs-workspace-id $LOG_WS_ID --logs-workspace-key $LOG_WS_KEY
   ```

4. **Create Azure Storage Account + File Shares:**
   ```bash
   STORAGE_ACCOUNT="cinebasefiles$(date +%s | tail -c 4)"
   
   az storage account create --name $STORAGE_ACCOUNT --resource-group $RESOURCE_GROUP --location $LOCATION --sku Standard_LRS --kind StorageV2
   STORAGE_KEY=$(az storage account keys list --resource-group $RESOURCE_GROUP --account-name $STORAGE_ACCOUNT --query "[0].value" --output tsv)
   
   # MariaDB data share
   az storage share create --name mariadb-data --account-name $STORAGE_ACCOUNT --account-key "$STORAGE_KEY" --quota 5
   
   # Data Protection Keys share
   az storage share create --name webapp-dataprotection --account-name $STORAGE_ACCOUNT --account-key "$STORAGE_KEY" --quota 1
   ```

5. **Save configuration to deployment config file:**
   - Create `azure-deploy-config.sh` with resource names, IDs, keys
   - Source in subsequent deployment scripts

6. **Verify resources created:**
   ```bash
   az acr list --resource-group $RESOURCE_GROUP
   az containerapp env list --resource-group $RESOURCE_GROUP
   az storage account list --resource-group $RESOURCE_GROUP
   ```

**Verifiche**:

- ACR login successful: `az acr login --name $ACR_NAME`
- ACA environment ready: `az containerapp env list` shows entry
- Storage account accessible: `az storage share list --account-name $STORAGE_ACCOUNT`

**Test automatici minimi**:

- ACR login succeeds
- ACA env accessible via CLI
- Storage shares created and listable

**Checklist fase**:

- [ ] Resource group created
- [ ] ACR created + login successful
- [ ] Log Analytics workspace created
- [ ] ACA environment created
- [ ] Storage account created
- [ ] File shares created (mariadb-data, webapp-dataprotection)
- [ ] Config saved to script file

---

### FASE 9 - Deployment database MariaDB su ACA

**Obiettivo**: Distribuire MariaDB container app in ACA con Azure Files persistent storage.

**Attività**:

1. **Tag and push backend image to ACR:**
   ```bash
   docker build -f backend/Dockerfile -t $ACR_LOGIN_SERVER/cinebase-backend:latest .
   docker push $ACR_LOGIN_SERVER/cinebase-backend:latest
   ```

2. **Create MariaDB container app:**
   ```bash
   MARIADB_APP_NAME="cinebase-db"
   MARIADB_PASSWORD="<SecureRandomPassword32Chars>"
   
   az containerapp create \
     --name $MARIADB_APP_NAME \
     --resource-group $RESOURCE_GROUP \
     --environment $ACA_ENV \
     --image mariadb:11.4 \
     --cpu 0.5 \
     --memory 1Gi \
     --min-replicas 0 \
     --max-replicas 1 \
     --secrets mariadb-password="$MARIADB_PASSWORD" \
     --env-vars \
       MYSQL_ROOT_PASSWORD=secretref:mariadb-password \
       MYSQL_DATABASE=cinebase \
     --azure-file-volume-account-name $STORAGE_ACCOUNT \
     --azure-file-volume-account-key "$STORAGE_KEY" \
     --azure-file-volume-share-name mariadb-data \
     --azure-file-volume-mount-path /var/lib/mysql \
     --ingress internal \
     --target-port 3306
   ```

3. **Verify MariaDB running:**
   ```bash
   az containerapp show --name $MARIADB_APP_NAME --resource-group $RESOURCE_GROUP --query properties.runningStatus
   ```

4. **Wait for container to be ready (~30s cold start):**
   ```bash
   az containerapp logs show --name $MARIADB_APP_NAME --resource-group $RESOURCE_GROUP --follow
   # Wait for "ready for connections"
   ```

**Verifiche**:

- Container app created and running
- Container logs show "ready for connections"
- Azure Files mount successful (check /var/lib/mysql accessible)

**Test automatici minimi**:

- Container status = Running
- No error logs in container

**Checklist fase**:

- [ ] Backend image pushed to ACR
- [ ] MariaDB container app created
- [ ] Secrets configured (mariadb-password)
- [ ] Azure Files mounted
- [ ] Container logs show ready
- [ ] Internal ingress configured (no public IP)

---

### FASE 10 - Deployment backend su ACA con configurazione secrets/env

**Obiettivo**: Deploy backend API container app con segreti, variabili ambiente, Data Protection Keys sharing.

**Attività**:

1. **Tag and push backend image to ACR:**
   ```bash
   docker build -f backend/Dockerfile -t $ACR_LOGIN_SERVER/cinebase-backend:latest .
   docker push $ACR_LOGIN_SERVER/cinebase-backend:latest
   ```

2. **Create secrets and env vars for backend:**
   ```bash
   BACKEND_APP_NAME="cinebase-api"
   DB_INTERNAL_URL="cinebase-db.internal.azurecontainerapps.io"
   CONNECTION_STRING="Server=$DB_INTERNAL_URL;Port=3306;Database=cinebase;Uid=root;Pwd=secretref:mariadb-password;..."
   
   # Prepare secrets array
   SECRETS=(
     "mariadb-password=$MARIADB_PASSWORD"
     "smtp-password=$SMTP_PASSWORD"
     "google-client-secret=$GOOGLE_OAUTH_CLIENT_SECRET"
     "microsoft-client-secret=$MICROSOFT_OAUTH_CLIENT_SECRET"
     "stripe-secret-key=$STRIPE_SECRET_KEY"
     "jwt-secret=$JWT_SECRET"
   )
   ```

3. **Create backend container app:**
   ```bash
   az containerapp create \
     --name $BACKEND_APP_NAME \
     --resource-group $RESOURCE_GROUP \
     --environment $ACA_ENV \
     --image $ACR_LOGIN_SERVER/cinebase-backend:latest \
     --registry-server $ACR_LOGIN_SERVER \
     --registry-username $(az acr credential show --name $ACR_NAME --query username --output tsv) \
     --registry-password $(az acr credential show --name $ACR_NAME --query passwords[0].value --output tsv) \
     --cpu 0.5 \
     --memory 1Gi \
     --min-replicas 0 \
     --max-replicas 3 \
     --secrets \
       mariadb-password="$MARIADB_PASSWORD" \
       smtp-password="$SMTP_PASSWORD" \
       google-client-secret="$GOOGLE_OAUTH_CLIENT_SECRET" \
       microsoft-client-secret="$MICROSOFT_OAUTH_CLIENT_SECRET" \
       stripe-secret-key="$STRIPE_SECRET_KEY" \
       jwt-secret="$JWT_SECRET" \
     --env-vars \
       ASPNETCORE_ENVIRONMENT=Production \
       ASPNETCORE_URLS=http://+:8080 \
       "ConnectionStrings__FilmDbContext=$CONNECTION_STRING" \
       SMTP_SERVER=smtp.gmail.com \
       SMTP_PORT=587 \
       "SMTP_USERNAME=$SMTP_USERNAME" \
       SMTP_PASSWORD=secretref:smtp-password \
       "SMTP_SENDER_EMAIL=$SMTP_SENDER_EMAIL" \
       GOOGLE_OAUTH_CLIENT_ID="$GOOGLE_OAUTH_CLIENT_ID" \
       GOOGLE_OAUTH_CLIENT_SECRET=secretref:google-client-secret \
       MICROSOFT_OAUTH_CLIENT_ID="$MICROSOFT_OAUTH_CLIENT_ID" \
       MICROSOFT_OAUTH_CLIENT_SECRET=secretref:microsoft-client-secret \
       STRIPE_PUBLIC_KEY="$STRIPE_PUBLIC_KEY" \
       STRIPE_SECRET_KEY=secretref:stripe-secret-key \
       JWT_SECRET=secretref:jwt-secret \
       "FRONTEND_BASE_URL=$FRONTEND_PUBLIC_URL" \
     --azure-file-volume-account-name $STORAGE_ACCOUNT \
     --azure-file-volume-account-key "$STORAGE_KEY" \
     --azure-file-volume-share-name webapp-dataprotection \
     --azure-file-volume-mount-path /mnt/dataprotection \
     --ingress external \
     --target-port 8080 \
     --transport http1 \
     --enable-session-affinity
   ```

4. **Update Program.cs for Data Protection Keys:**
   ```csharp
   // In Program.cs before app build
   if (!app.Environment.IsDevelopment())
   {
     var dpKeysPath = Environment.GetEnvironmentVariable("DATA_PROTECTION_KEYS_PATH") ?? "/mnt/dataprotection";
     builder.Services.AddDataProtection()
       .SetApplicationName("CineBase")
       .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath));
   }
   ```

5. **Verify backend deployed:**
   ```bash
   az containerapp show --name $BACKEND_APP_NAME --resource-group $RESOURCE_GROUP --query properties.configuration.ingress.fqdn
   BACKEND_FQDN=$(az containerapp show --name $BACKEND_APP_NAME --resource-group $RESOURCE_GROUP --query properties.configuration.ingress.fqdn --output tsv)
   ```

6. **Test backend health:**
   ```bash
   curl -v https://$BACKEND_FQDN/health
   ```

**Verifiche**:

- Backend container app created and running
- FQDN accessible via public internet
- Health endpoint responds (200 or 500 depending on DB cold start)
- Data Protection Keys mounted and writable

**Test automatici minimi**:

- Container app status = Running
- Health endpoint responds
- No auth 500 errors (auth service loads)

**Checklist fase**:

- [ ] Backend image built + pushed to ACR
- [ ] Secrets configured in ACA
- [ ] Environment variables mapped correctly
- [ ] Data Protection Keys share mounted
- [ ] Session affinity enabled
- [ ] Container app running
- [ ] FQDN publicly accessible
- [ ] Health check passes

---

### FASE 11 - Deployment frontend su ACA con FQDN e domain personalizzato

**Obiettivo**: Deploy frontend static app in ACA, configurare dominio personalizzato opzionale.

**Attività**:

1. **Tag and push frontend image to ACR:**
   ```bash
   docker build -f frontend/Dockerfile -t $ACR_LOGIN_SERVER/cinebase-frontend:latest .
   docker push $ACR_LOGIN_SERVER/cinebase-frontend:latest
   ```

2. **Create frontend container app:**
   ```bash
   FRONTEND_APP_NAME="cinebase-web"
   
   az containerapp create \
     --name $FRONTEND_APP_NAME \
     --resource-group $RESOURCE_GROUP \
     --environment $ACA_ENV \
     --image $ACR_LOGIN_SERVER/cinebase-frontend:latest \
     --registry-server $ACR_LOGIN_SERVER \
     --registry-username $(az acr credential show --name $ACR_NAME --query username --output tsv) \
     --registry-password $(az acr credential show --name $ACR_NAME --query passwords[0].value --output tsv) \
     --cpu 0.5 \
     --memory 512Mi \
     --min-replicas 1 \
     --max-replicas 3 \
     --env-vars \
       FRONTEND_PORT=5001 \
       "BACKEND_API_URL=https://$BACKEND_FQDN" \
     --ingress external \
     --target-port 5001 \
     --transport http1
   ```

3. **Get frontend FQDN:**
   ```bash
   FRONTEND_FQDN=$(az containerapp show --name $FRONTEND_APP_NAME --resource-group $RESOURCE_GROUP --query properties.configuration.ingress.fqdn --output tsv)
   echo "Frontend URL: https://$FRONTEND_FQDN"
   ```

4. **Configure custom domain (optional):**
   - Register domain with DNS provider (Namecheap, GoDaddy, etc.)
   - In Azure Portal: Frontend Container App → Ingress → Custom domain
   - Add DNS records per Azure instructions (usually CNAME + TXT validation)
   - Azure issues free SSL cert automatically
   - Update FRONTEND_BASE_URL in backend env var se necessario

5. **Update CORS in backend:**
   - If custom domain different from FQDN, add to CORS allowed origins in Program.cs

6. **Verify frontend deployed:**
   ```bash
   curl -v https://$FRONTEND_FQDN/
   ```

**Verifiche**:

- Frontend container app created and running
- FQDN accessible via https
- Static assets load (CSS, JS)
- No 404s for assets

**Test automatici minimi**:

- Frontend app status = Running
- GET / returns 200 + HTML content
- Assets accessible (CSS returns non-empty file)

**Checklist fase**:

- [ ] Frontend image built + pushed to ACR
- [ ] Container app created
- [ ] FQDN publicly accessible
- [ ] Static assets served correctly
- [ ] HTTPS certificate auto-issued
- [ ] Custom domain configured (optional)
- [ ] CORS configured if needed

---

### FASE 12 - Test e smoke verification ACA end-to-end

**Obiettivo**: Verificare stack completo ACA: backend + frontend + database integration.

**Attività**:

1. **Smoke test script `azure-smoke-test.sh`:**
   ```bash
   #!/bin/bash
   BACKEND_URL="https://cinebase-api.*.azurecontainerapps.io"
   FRONTEND_URL="https://cinebase-web.*.azurecontainerapps.io"
   
   echo "Testing Backend..."
   curl -v $BACKEND_URL/health
   curl -v $BACKEND_URL/auth/me  # expect 401
   
   echo "Testing Frontend..."
   curl -v $FRONTEND_URL/ | grep -q "html" && echo "OK" || echo "FAIL"
   
   echo "Testing Auth Flow..."
   # POST /auth/register, verify response
   # POST /auth/login, verify token issued
   
   echo "Testing Database Connectivity..."
   # Query via API: GET /admin/utenti (should 401 or 200 depending on state)
   ```

2. **End-to-end flow test:**
   - Open frontend in browser
   - Navigate to login
   - Click register, create account
   - Login with new account
   - Browse catalog
   - Initiate checkout (if flow requires)
   - Verify profilo page loads

3. **Performance baseline:**
   - Document first load time (~3-5s expected for cold start)
   - Database query latency
   - API response time /catalog

4. **Error handling:**
   - Intentionally cause error (e.g., invalid DB password, expired token)
   - Verify error page displayed, not blank

5. **Logs verification:**
   ```bash
   az containerapp logs show --name cinebase-api --resource-group cinebase-rg --follow
   az containerapp logs show --name cinebase-web --resource-group cinebase-rg --follow
   az containerapp logs show --name cinebase-db --resource-group cinebase-rg --follow
   ```
   - No ERROR level logs
   - Expected INFO logs for requests

6. **Health and auto-healing:**
   - Stop frontend container (simulate failure)
   - Verify ACA auto-restarts within 60s
   - Verify FQDN still accessible (new instance serving)

**Verifiche**:

- All smoke tests pass
- No errors in logs
- Frontend renders correctly
- API responds with correct status codes
- Database queries work (books, shows accessible)

**Test automatici minimi**:

- Smoke test script all checks pass
- No 5xx errors in backend
- Frontend loads + renders

**Checklist fase**:

- [ ] Smoke test script funzionante
- [ ] End-to-end flow tested in browser
- [ ] Performance baseline documented
- [ ] Logs reviewed for errors
- [ ] Auto-healing verified
- [ ] No blocking issues found

---

### FASE 13 - Documentazione deployment, troubleshooting e runbook

**Obiettivo**: Documentare setup, deployment, troubleshooting per team.

**Attività**:

1. **Creare `docs/DOCKER_LOCAL_SETUP.md`:**
   - Prerequisites (Docker Desktop, version)
   - Clone repo + .env.example → .env.local
   - `bash docker-compose-init.sh` comando
   - Accessible URLs + credentials
   - Common issues + solutions (volume permission, port conflict, etc.)
   - Cleanup commands

2. **Creare `docs/AZURE_DEPLOYMENT.md`:**
   - Prerequisites (Azure CLI, subscription)
   - Step-by-step Azure setup (ACR, ACA env, Storage)
   - Build + push images to ACR
   - Deploy database MariaDB
   - Deploy backend API
   - Deploy frontend web
   - Configure custom domain (optional)
   - Verify deployment

3. **Creare `azure-deploy.sh` script (automated):**
   - Source configuration
   - Create all resources
   - Build + push images
   - Deploy containers
   - Print final URLs

4. **Creare `docs/TROUBLESHOOTING.md`:**
   - Common errors:
     - Container failed to start → check logs
     - Database connection refused → check internal URL, secretref
     - Frontend can't reach backend → check BACKEND_API_URL, CORS
     - Session not persisting across requests → check Data Protection Keys mount
     - Cold start timeout → increase health check timeout
   - Debug commands (logs, exec, scaling, resource usage)
   - Performance tuning (CPU, memory, replicas)

5. **Aggiornare main `README.md`:**
   - Quick start links (Docker local, Azure deployment)
   - Architecture diagram (local vs. cloud)
   - Branch strategy (dev_iteration_6)
   - Contributing guidelines

6. **Documentare ambiente variables per ciascun environment:**
   - `.env.example` per local development
   - `.env.docker` per docker-compose (subset di .env.example, only docker-relevant)
   - `.env.azure` per Azure deployment (template, secrets as secretref)

7. **Documentare security best practices:**
   - Never commit .env files with real secrets
   - ACR credentials rotation schedule
   - Data Protection Keys backup strategy
   - SSL certificate auto-renewal (handled by Azure)

**Verifiche**:

- Documentazione completa e accurate
- Setup instructions reproducible da zero
- Troubleshooting covers common issues
- Scripts automated e tested

**Test automatici minimi**:

- README links valid
- DOCKER_LOCAL_SETUP.md commands work end-to-end
- AZURE_DEPLOYMENT.md steps reproducible

**Checklist fase**:

- [ ] `DOCKER_LOCAL_SETUP.md` creato
- [ ] `AZURE_DEPLOYMENT.md` creato
- [ ] `TROUBLESHOOTING.md` creato
- [ ] `azure-deploy.sh` script funzionante
- [ ] `.env.example` + `.env.docker` + `.env.azure` documentati
- [ ] README.md aggiornato
- [ ] Security best practices documentati
- [ ] All docs reviewed per accuratezza

---

## 6) File da Creare / Modificare

### Nuovi file:

```
backend/Dockerfile
backend/.dockerignore
backend/scripts/FilmApiSeeder/Dockerfile
frontend/Dockerfile
frontend/.dockerignore
docker-compose.yml
docker-compose-init.sh
docker-compose-smoke-test.sh
.env.example
.env.local.example (opzionale)
.env.docker
.env.azure (template)
azure-deploy.sh
azure-smoke-test.sh
azure-deploy-config.sh
docs/DOCKER_LOCAL_SETUP.md
docs/AZURE_DEPLOYMENT.md
docs/TROUBLESHOOTING.md
docs/ARCHITECTURE.md (containerizzazione)
```

### File modificati:

```
backend/FilmAPI/Program.cs
  - Add EF Core migration auto-run
  - Add Data Protection Keys configuration
  - Add health check endpoint (/health)
  - Support env var for DB connection, secrets

frontend/CineBase.Web/wwwroot/js/auth.js
  - No breaking changes
  - Ensure BACKEND_API_URL env var respected

README.md
  - Add Docker local + Azure deployment sections

.gitignore
  - Add .env.local, .env.docker (local file, non-template)
  - Add azure-deploy-config.sh (generated)
```

---

## 7) Criteri di Accettazione

Tutti i criteri devono essere verificati e approvati prima di considerare l'iterazione completata.

### Containerizzazione Locale ✓

- [ ] `docker-compose up -d` from repo root avvia tutti i servizi in <45s
- [ ] Database inizializza schema + admin user + seeded films on startup
- [ ] Backend API accessible su `http://localhost:5000`, health OK
- [ ] Frontend accessible su `http://localhost:5001`, HTML loads
- [ ] Tutti gli endpoint auth/checkout/profilo funzionano come pre-container
- [ ] Login locale + social login (Google/Microsoft) funzionano
- [ ] Password reset flow via email works
- [ ] Ticket PDF generation works
- [ ] Admin user interface per gestione utenti works
- [ ] Seeder runs idempotently (no duplicates on re-run)
- [ ] No secrets exposed in container logs or compose file
- [ ] `docker-compose down -v && docker-compose up` produces clean state

### Testing ✓

- [ ] Backend integration tests pass (existing tests still green)
- [ ] Smoke test suite `docker-compose-smoke-test.sh` all checks pass
- [ ] Frontend loads without console errors
- [ ] No 404s for static assets
- [ ] Database connectivity verified from all services
- [ ] Health checks on all services respond correctly

### Azure Deployment ✓

- [ ] ACR created and images pushed successfully
- [ ] ACA environment created with Log Analytics
- [ ] Azure Files storage account + shares created
- [ ] MariaDB container app deployed + ready (cold start <60s)
- [ ] Backend container app deployed + health responding
- [ ] Frontend container app deployed + HTTPS working
- [ ] Session affinity enabled on backend
- [ ] Data Protection Keys shared via Azure Files
- [ ] Custom domain configured (if desired)
- [ ] SSL certificate issued and valid
- [ ] All container apps have public/internal ingress configured correctly

### Smoke Testing Azure ✓

- [ ] Backend API FQDN accessible, health endpoint responds
- [ ] Frontend FQDN accessible, index.html loads
- [ ] Register + login flow works end-to-end
- [ ] Catalog browsing works
- [ ] Admin user can manage other users
- [ ] No 5xx errors in Azure logs
- [ ] Performance acceptable (<5s for page load)

### Documentation ✓

- [ ] `DOCKER_LOCAL_SETUP.md` complete with prerequisites, commands, troubleshooting
- [ ] `AZURE_DEPLOYMENT.md` complete with step-by-step guide
- [ ] `TROUBLESHOOTING.md` covers common issues + solutions
- [ ] `.env.example` documented with ALL variables
- [ ] README.md updated with Docker + Azure links
- [ ] Architecture documentation includes containerization + Azure

### No Regressions ✓

- [ ] All Iterazione 5 features work identically (auth, checkout, profilo, admin)
- [ ] No breaking changes to API endpoints
- [ ] No breaking changes to frontend routes
- [ ] Existing test suite passes (at least 95%+ before → after)
- [ ] Database schema unchanged (only migrations added, no removal)

---

## 8) Test Strutturati da Sviluppare

### Unit Tests (no changes - leverage existing)

- Backend integration tests continue to pass
- Auth service tests
- Checkout service tests
- Admin service tests

### Container-Specific Integration Tests

**New test file:** `tests/backend/ContainerIntegrationTests.cs`

```csharp
[TestClass]
public class DockerComposeIntegrationTests
{
  // Test database connection from container
  [TestMethod]
  public async Task DatabaseConnectsSuccessfully()
  {
    // Arrange: connection string from env (docker-compose provides)
    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__FilmDbContext");
    
    // Act + Assert: Can query users
    using var context = new FilmDbContext(new DbContextOptions<FilmDbContext>(...));
    var userCount = await context.Users.CountAsync();
    Assert.IsTrue(userCount > 0);
  }
  
  // Test migrations applied
  [TestMethod]
  public async Task MigrationsAppliedSuccessfully()
  {
    // Assert: All migration tables exist
    // SELECT COUNT(*) FROM __EFMigrationsHistory
  }
  
  // Test seeder creates consistent data
  [TestMethod]
  public async Task SeederCreatesValidData()
  {
    // Assert: films count > 100
    // Assert: cinemas count > 0
    // Assert: admin user exists
  }
}
```

### Smoke Test Scripts

**`docker-compose-smoke-test.sh`:**

- Check all service health endpoints
- Verify database accessible
- Test auth register/login
- Test frontend loads
- Check asset loading

**`azure-smoke-test.sh`:**

- Check backend + frontend FQDNs
- Test health endpoints
- Test auth flows
- Check logs for errors

### Performance Baseline Tests

- Document first load time
- Database query latency
- API response time for catalog
- Frontend asset load time

---

## 9) Note Implementative Speciali

### EF Core Migrations Auto-Run

In `Program.cs` before `app.Run()`:

```csharp
using (var scope = app.Services.CreateScope())
{
  var context = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
  await context.Database.MigrateAsync();
  logger.LogInformation("Database migrations applied");
}
```

**Implication:** Migrations run on every backend startup. Ensure idempotent (EF Core handles this, but verify in tests).

### Data Protection Keys Sharing

Add to `Program.cs`:

```csharp
if (!app.Environment.IsDevelopment())
{
  var keysPath = Environment.GetEnvironmentVariable("DATA_PROTECTION_KEYS_PATH") ?? "/mnt/dataprotection";
  builder.Services.AddDataProtection()
    .SetApplicationName("CineBase")
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}
```

**Implication:** Local dev uses default (Windows DPAPI / Linux user directory), Azure uses shared mount. Ensure directory writable.

### Idempotent Seeder

Seeder must upsert, not insert:

```csharp
// Bad
db.Films.Add(new Film { TmdbId = 123, Title = "..." });
await db.SaveChangesAsync();

// Good
var existing = await db.Films.FirstOrDefaultAsync(f => f.TmdbId == 123);
if (existing == null)
{
  db.Films.Add(new Film { TmdbId = 123, Title = "..." });
}
else
{
  existing.Title = "..."; // update if needed
}
await db.SaveChangesAsync();
```

### Docker-compose depends_on with healthcheck

`depends_on: service_healthy` waits for healthcheck to pass, not just container starting:

```yaml
backend:
  depends_on:
    db:
      condition: service_healthy  # waits for mysqladmin ping success
```

**Implication:** Must define healthcheck on db service. If no healthcheck, use `condition: service_started` (less safe).

---

## 10) Timeline Stimato

(Basato su team size ~2 developers)

| Fase | Sforzo Stimato | Note |
| --- | --- | --- |
| FASE 0 | 2-4 ore | Analisi + inventory |
| FASE 1 | 4-6 ore | Backend Dockerfile + layer optimization |
| FASE 2 | 3-4 ore | Frontend Dockerfile + Tailwind build |
| FASE 3 | 2-3 ore | Seeder Dockerfile |
| FASE 4 | 3-4 ore | docker-compose.yml structure |
| FASE 5 | 2-3 ore | .env management |
| FASE 6 | 4-6 ore | Orchestrazione + init script |
| FASE 7 | 4-6 ore | Smoke testing locale |
| FASE 8 | 3-4 ore | Azure resources setup |
| FASE 9 | 2-3 ore | MariaDB deployment ACA |
| FASE 10 | 3-4 ore | Backend deployment ACA |
| FASE 11 | 2-3 ore | Frontend deployment ACA |
| FASE 12 | 4-6 ore | End-to-end testing + troubleshooting |
| FASE 13 | 4-6 ore | Documentation |
| **TOTAL** | **~50-65 ore** | ~2 weeks per 1 FTE, or ~1 week per 2 FTE |

---

## 11) Rischi Mitigazione

| Rischio | Probabilità | Mitigazione |
| --- | --- | --- |
| Database migrations conflict | Bassa | Ensure migrations only ADD tables/columns; test migration in fresh DB |
| Seeder duplicate data | Media | Implement upsert logic, test idempotency |
| Cold start timeout ACA | Bassa | Increase health check timeout, implement retry in backend |
| Data Protection Keys not writable | Bassa | Pre-create directory in Azure Files; ensure mount permissions |
| Secrets accidentally logged | Media | Use secretref always; audit logging code |
| Performance regression | Media | Document baseline pre-container; smoke test ACA |
| CORS issues frontend-backend | Bassa | Pre-configure CORS in Program.cs for prod FQDN |
| Storage account costs | Bassa | Monitor Azure Files usage; start with 5 GB quota |

---

## Conclusione

Questa Iterazione 6 trasforma CineBase in un'applicazione cloud-native deployable su infrastruttura enterprise Azure, mantenendo la compatibilità con lo sviluppo locale via docker-compose. La containerizzazione è il prerequisito per CI/CD, scaling orizzontale e production-grade availability.

**Next Iterations:**
- Iterazione 7: CI/CD GitHub Actions (build → test → deploy)
- Iterazione 8: Monitoring avanzato + APM (Application Insights)
- Iterazione 9: Backup + Disaster Recovery strategy
