# Piano di Lavoro - Iterazione 6: Containerizzazione Completa e Deploy su Azure Container Apps

**Autore**: Sisyphus
**Branch target**: `dev_iteration_6`
**Data**: 2026-06-04

---

## Stato Avanzamento Fasi

| Fase | Stato | Data | Note |
| --- | --- | --- | --- |
| FASE 1 — Refinement Dockerfile multistage e docker-compose | **Pianificata** | - | Ottimizzazione Dockerfile esistenti, orchestrazione container, init DB, seeder automatico |
| FASE 2 — Configurazione iniziativa "clone & run" con .env e persistenza | **Pianificata** | - | Script init DB, admin seed, FilmApiSeeder automatico, maildev per sviluppo |
| FASE 3 — Healthcheck, dependency ordering e startup robusta | **Pianificata** | - | Healthcheck su tutti i servizi, wait-for-it pattern, retry policy |
| FASE 4 — ACA Infrastructure as Code (IaC) | **Pianificata** | - | Script PowerShell/CLI per creazione risorse Azure (ACR, ACA Env, Storage, etc.) |
| FASE 5 — Build e Push immagini su ACR | **Pianificata** | - | Build multi-arch, push immagini, version tagging |
| FASE 6 — Deploy MariaDB su ACA con Azure Files | **Pianificata** | - | Container App MariaDB, volume persistente, ingress interno |
| FASE 7 — Deploy Backend API su ACA | **Pianificata** | - | Container App backend, secrets, env vars, CORS, health probe |
| FASE 8 — Deploy Seeder come ACA Job | **Pianificata** | - | ACA Job one-shot, TMDB seeding, cleanup opzionale |
| FASE 9 — Deploy Frontend su ACA con Data Protection | **Pianificata** | - | Container App frontend, session affinity, Azure Files per Data Protection Keys |
| FASE 10 — Configurazione OAuth, SMTP, Dominio Personalizzato | **Pianificata** | - | Redirect URI aggiornati, certificato gestito, SMTP per email |
| FASE 11 — Test end-to-end, verifica e documentazione | **Pianificata** | - | Smoke test locale + ACA, aggiornamento status.md/changelog.md |

---

## 1) Obiettivo Iterazione

L'Iterazione 6 ha **due obiettivi principali** per portare CineBase dalla fase di sviluppo locale a un prodotto deployabile su cloud:

### Obiettivo 1: Containerizzazione Completa (docker-compose)

Ottenere una versione completamente conteinerizzata dell'app gestita localmente mediante docker-compose secondo le best practices. L'obiettivo è che **un nuovo sviluppatore** che clona il repository possa eseguire `docker-compose up -d` e ottenere:

- Database MariaDB con dati persistenti
- Backend API .NET 9 con migrazioni automatiche e seed dati amministrativi
- Frontend ASP.NET Core che serve pagine statiche
- FilmApiSeeder che popola il database con film, cinema, sale e show da TMDB
- Account admin preconfigurato (`admin@cinebase.it` / `Admin123!`)
- Servizio email fittizio (MailDev) per sviluppo locale
- Configurazione OAuth pronta (Google/Microsoft) quando le credenziali sono fornite
- Nessuna dipendenza da dati locali preesistenti

### Obiettivo 2: Deploy su Azure Container Apps (ACA)

Effettuare il deployment dell'app CineBase su Azure Container Apps seguendo un approccio analogo alla guida di riferimento EducationalGames, adattato all'architettura CineBase:

- Backend API con ingress interno (raggiungibile solo dalla rete ACA)
- Frontend con ingress esterno (pubblico) e session affinity
- MariaDB su Azure Files per persistenza
- Seeder come ACA Job one-shot
- Segreti gestiti tramite ACA Secrets
- Configurazione OAuth, SMTP e dominio personalizzato

---

## 2) Architettura Target

### 2.1 Architettura Locale (docker-compose)

```
┌─────────────────────────────────────────────────────┐
│                    docker-compose                     │
│                                                       │
│  ┌──────────┐   ┌──────────┐   ┌──────────────────┐  │
│  │  MailDev  │   │  MariaDB │   │ FilmApiSeeder     │  │
│  │ :1080     │   │ :3306    │   │ (one-shot)        │  │
│  │ :1025     │   │          │   │                    │  │
│  └──────────┘   └────┬─────┘   └────────┬───────────┘  │
│                      │                  │              │
│                      ▼                  ▼              │
│              ┌───────────────────────────────┐         │
│              │  Backend API (cinebase-api)     │         │
│              │  :5000 (host) → :8080          │         │
│              └───────────────┬───────────────┘         │
│                              │                         │
│                              ▼                         │
│              ┌───────────────────────────────┐         │
│              │  Frontend (cinebase-web)       │         │
│              │  :5001 (host) → :8080          │         │
│              └───────────────────────────────┘         │
│                                                       │
│  Volumi:                                              │
│  - cinebase_db_data:/var/lib/mysql                   │
│  - ./backend/FilmAPI/Media:/app/media:ro             │
│  - cinebase_maildev_data:/var/mail                  │
└─────────────────────────────────────────────────────┘
```

### 2.2 Architettura ACA (Azure Container Apps)

```
                    Internet
                        │
                        ▼
           ┌───────────────────────┐
           │  Frontend (ext)       │
           │  cinebase-web         │
           │  :8080 → HTTPS        │
           └───────────┬───────────┘
                       │ BACKEND_API_URL (internal)
                       ▼
           ┌───────────────────────┐
           │  Backend API (int)    │
           │  cinebase-api         │
           │  :8080                │
           └───────────┬───────────┘
                       │ DB_HOST (internal)
                       ▼
           ┌───────────────────────┐
           │  MariaDB (int)        │
           │  cinebase-db          │
           │  :3306                │
           │  Volume: Azure Files  │
           └───────────────────────┘

  Seeder (one-shot ACA Job):
  ┌─────────────────────────────────────────────────┐
  │  cinebase-seeder                                 │
  │  → popola DB da TMDB, crea admin, categorie     │
  └─────────────────────────────────────────────────┘
```

### 2.3 Servizi e Dipendenze

| Servizio | Nome Container | Immagine | Porte | Dipende da |
|----------|---------------|----------|-------|------------|
| Database | cinebase-db | mariadb:11.4 | 3306 | - |
| MailDev | cinebase-maildev | maildev/maildev | 1080, 1025 | - |
| Seeder | cinebase-seeder | cinebase-seeder:latest | - | db (healthy) |
| Backend API | cinebase-api | cinebase-api:latest | 8080 | db (healthy), seeder (completed) |
| Frontend | cinebase-web | cinebase-web:latest | 8080 | backend (started) |

**Ordine di avvio docker-compose:**
1. `db` + `maildev` (parallelo)
2. `seeder` → attende `db` healthy, poi termina
3. `backend` → attende `db` healthy + `seeder` completed
4. `frontend` → attende `backend` started

---

## 3) Requisiti e Vincoli

### 3.1 Requisiti Funzionali

1. **RF1**: `docker-compose up -d` deve produrre un'applicazione funzionante con film, cinema, sale, show seedati da TMDB.
2. **RF2**: L'account admin (`admin@cinebase.it` / `Admin123!`) deve essere configurato automaticamente.
3. **RF3**: Il servizio email fittizio (MailDev) deve funzionare per lo sviluppo locale senza SMTP esterno.
4. **RF4**: I provider OAuth (Google/Microsoft) devono essere configurabili tramite `.env`.
5. **RF5**: Il database deve persistere tra riavvii (volume Docker nominato).
6. **RF6**: Il seeder deve essere eseguibile on-demand oltre che automaticamente all'avvio.
7. **RF7**: L'applicazione deployata su ACA deve funzionare con frontend pubblico e backend interno.
8. **RF8**: Le Data Protection Keys del frontend devono essere condivise su Azure Files per supportare scaling orizzontale.

### 3.2 Vincoli di Implementazione

1. **V1**: I Dockerfile devono essere multistage e ottimizzati (layer caching, riduzione dimensione immagine).
2. **V2**: Non devono essere usati dati da un database locale preesistente — scenario "clone & run" reale.
3. **V3**: I segreti non devono mai essere hardcodati nei Dockerfile o nel docker-compose.
4. **V4**: Le immagini per ACA devono essere buildate con `az acr build` o Docker locale e pushati su ACR.
5. **V5**: Il frontend deve chiamare il backend via internal ingress ACA (BACKEND_API_URL).
6. **V6**: Il backend deve esporre CORS configurato per l'URL del frontend su ACA.
7. **V7**: Tutte le variabili sensibili in ACA devono essere gestite come secrets, non env vars in chiaro.

### 3.3 Criteri di Accettazione Generali

1. Un utente che clona il repo ed esegue `docker-compose up -d` ottiene un'app funzionante.
2. `docker-compose logs seeder` mostra seeding completato con film, cinema, sale.
3. Login con `admin@cinebase.it` / `Admin123!` funziona al primo avvio.
4. Il seeder può essere rieseguito on-demand per aggiornare film/show.
5. Lo script di deploy ACA crea tutte le risorse Azure e deploya l'app.
6. L'app ACA è raggiungibile pubblicamente tramite URL ACA.
7. Login/registrazione/acquisto funzionano su ACA.
8. I dati persistono dopo riavvio dei container ACA.

---

## 4) Stato Attuale e Diagnostica

### 4.1 Dockerfile Esistenti

**Backend** (`backend/Dockerfile`):
- Già multistage (build `sdk:9.0` → runtime `aspnet:9.0`)
- Espone porta 8080
- **Mancanze**: nessun healthcheck, nessun layer ottimizzato per `restore` separato (già presente), nessun tag di versione

**Frontend** (`frontend/CineBase.Web/Dockerfile`):
- Già multistage
- Espone porta 8080
- **Mancanze**: stessa situazione del backend

**Seeder** (`backend/scripts/FilmApiSeeder/Dockerfile`):
- Già multistage
- Usa `aspnet:9.0` come runtime (anche se è una console app)
- **Mancanze**: potrebbe usare immagine runtime più piccola (`sdk:9.0` non serve in produzione)

### 4.2 docker-compose.yml Esistente

- Già definisce 4 servizi: `db`, `backend`, `frontend`, `seeder`
- Usa healthcheck per MariaDB
- Dipendenze ordinate correttamente
- **Mancanze**: manca servizio MailDev, mancano variabili per OAuth/SMTP complete, manca init DB script per garanzia primo avvio

### 4.3 ACA Infrastructure Esistente

- `infra/azure/ACA-DEPLOY-GUIDE.md`: guida di deploy già scritta, da integrare nel piano
- `infra/azure/aca-deploy.ps1`: script PowerShell già scritto, da validare/testare
- **Mancanze**: lo script usa sintassi volume `--set` che potrebbe non funzionare su tutte le versioni ACA extension

---

## 5) Fasi di Implementazione

---

### FASE 1 — Refinement Dockerfile Multistage e docker-compose

**Obiettivo**: Ottimizzare i Dockerfile esistenti e raffinare docker-compose per lo scenario "clone & run".

**Attività**:

1. **Refinement `backend/Dockerfile`**:
   - Aggiungere etichette (labels) per tracciabilità (`org.opencontainers.image.version`, `org.opencontainers.image.description`)
   - Ottimizzare layer caching: confermare che `COPY csproj → restore` sia separato dal `COPY .`
   - Aggiungere `HEALTHCHECK` con `curl` o script HTTP sull'endpoint `/health`
   - Verificare che l'immagine runtime non contenga strumenti di build
   - Configurare `USER app` per esecuzione non-root (security best practice)

   ```dockerfile
   # Aggiunte chiavi rispetto all'esistente:
   LABEL org.opencontainers.image.title="CineBase API" \
         org.opencontainers.image.description="Backend API for CineBase film management" \
         org.opencontainers.image.version="1.0.0"
   
   HEALTHCHECK --interval=30s --timeout=3s --start-period=30s --retries=3 \
     CMD curl --fail http://localhost:8080/health || exit 1
   
   USER app
   ```

2. **Refinement `frontend/CineBase.Web/Dockerfile`**:
   - Stesse ottimizzazioni del backend (labels, USER app, HEALTHCHECK)
   - L'healthcheck frontend può verificare che `/index.html` sia servito correttamente
   - Aggiungere `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` per supporto proxy/load balancer

3. **Refinement `backend/scripts/FilmApiSeeder/Dockerfile`**:
   - Cambiare runtime stage in `mcr.microsoft.com/dotnet/runtime:9.0` invece di `aspnet:9.0` (è una console app, non serve ASP.NET)
   - Rimuovere dipendenze web non necessarie
   - Aggiungere labels

4. **Aggiornamento `docker-compose.yml`**:
   - Aggiungere servizio `maildev` per email fittizie in sviluppo locale
   - Configurare il backend per usare MailDev di default (`SMTP_HOST=maildev`, `SMTP_PORT=1025`)
   - Aggiungere variabili d'ambiente mancanti rispetto all'attuale docker-compose:
     - `ALLOWED_CORS_ORIGINS` per il backend
     - `SMTP_*` complete per il backend
     - `GOOGLE_OAUTH_*` / `MICROSOFT_OAUTH_*` per OAuth
     - `STRIPE_*` per pagamenti
   - Aggiungere `restart: unless-stopped` per servizi che devono sempre stare su
   - Aggiungere dipendenza `frontend` → `backend` con `condition: service_started`
   - Verificare che Media volume sia montato correttamente

5. **Verifica build**:
   ```bash
   docker build -t cinebase-api -f backend/Dockerfile backend
   docker build -t cinebase-web -f frontend/CineBase.Web/Dockerfile frontend/CineBase.Web
   docker build -t cinebase-seeder -f backend/scripts/FilmApiSeeder/Dockerfile backend
   ```

**File da creare/modificare**:
- `backend/Dockerfile` — MODIFICARE: labels, USER app, HEALTHCHECK
- `frontend/CineBase.Web/Dockerfile` — MODIFICARE: labels, USER app, HEALTHCHECK, FORWARDEDHEADERS
- `backend/scripts/FilmApiSeeder/Dockerfile` — MODIFICARE: immagine runtime `runtime:9.0`, labels
- `docker-compose.yml` — MODIFICARE: aggiungere maildev, variabili mancanti, restart policy

**Criteri di accettazione fase**:
1. `docker build` per i 3 Dockerfile completa senza errori
2. Le immagini sono significativamente più piccole (verificare con `docker images`)
3. L'healthcheck è presente nei Dockerfile backend e frontend
4. I container girano con utente non-root (`USER app`)
5. docker-compose si avvia senza errori di sintassi

**Test fase**:
- Build test: `docker build` per ogni Dockerfile
- Layer inspection: `docker history cinebase-api` per verificare layer caching
- Security check: verificare `USER app` nei runtime stage

---

### FASE 2 — Configurazione "Clone & Run" con .env e Persistenza

**Obiettivo**: Garantire che un nuovo sviluppatore possa eseguire `docker-compose up -d` e ottenere un'applicazione completamente funzionante.

**Attività**:

1. **Refinement `.env.example`** (radice progetto):
   - Aggiornare con TUTTE le variabili d'ambiente necessarie
   - Commentare chiaramente quali sono obbligatorie e quali opzionali
   - Aggiungere sezioni per: DB, JWT, Admin, SMTP/MailDev, OAuth, Stripe, TMDB

2. **Refinement `backend/.env.example`**:
   - Allineare con le variabili effettivamente lette da `Program.cs` e `FilmApiSeeder`
   - Documentare i valori di default per sviluppo locale

3. **Script `init-db.sh` / `init-db.sql`** (opzionale):
   - Valutare se serve uno script SQL iniziale per MariaDB
   - Al momento il seeder e il DataSeeder di FilmAPI gestiscono tutto via EF Core Migrations
   - Documentare che le migration sono automatiche all'avvio del backend

4. **Configurazione MailDev**:
   - MailDev fornisce:
     - SMTP server su porta 1025 (nessuna autenticazione richiesta)
     - Web interface su porta 1080 per visualizzare le email inviate
   - Configurare backend con `SMTP_HOST=maildev`, `SMTP_PORT=1025`, nessuna credenziale
   - Esporre porta 1080 in docker-compose per accesso web

5. **Configurazione Network docker-compose**:
   - Usare rete `cinebase-network` (già default bridge, ma esplicito è meglio)
   - Garantire che tutti i servizi possano comunicare tra loro via nome container

6. **Volume per Media**:
   - Verificare che `./backend/FilmAPI/Media:/app/media:ro` funzioni su tutti i sistemi (Windows, macOS, Linux)
   - Se il path non esiste, crearlo automaticamente o gestire l'assenza

**File da creare/modificare**:
- `.env.example` — MODIFICARE: completo di tutte le variabili
- `backend/.env.example` — MODIFICARE: allineato con le variabili lette
- `docker-compose.yml` — MODIFICARE: rete esplicita, volume maildev, porte maildev
- `README.md` (radice) — CREARE se non esiste, con istruzioni "Quick Start" per docker-compose

**Criteri di accettazione fase**:
1. Copiando `.env.example` in `.env` e modificando solo i valori obbligatori segnati, docker-compose funziona
2. MailDev interfaccia web è raggiungibile su `http://localhost:1080`
3. Le email inviate dall'app (reset password, biglietti) appaiono in MailDev
4. Il backend si connette a MailDev correttamente senza credenziali

**Test fase**:
- Test docker-compose con `.env.example` → copia → `docker-compose up -d`
- Verifica MailDev UI: `curl http://localhost:1080` → 200
- Verifica che una email di test (es. forgot password) appaia in MailDev

---

### FASE 3 — Healthcheck, Dependency Ordering e Startup Robusta

**Obiettivo**: Assicurare che l'applicazione si avvii in modo affidabile anche in caso di ritardi (latenza DB, seeder lento).

**Attività**:

1. **Aggiungere endpoint `/health` al backend**:
   - Creare endpoint `GET /health` in `Program.cs` che verifichi:
     - Connessione al DB (ping)
     - Stato generale dell'applicazione
   - L'healthcheck Docker chiamerà questo endpoint

2. **Refinement healthcheck MariaDB**:
   - L'attuale healthcheck è già robusto: `healthcheck.sh --connect --innodb_initialized`
   - Verificare che `start_period` sia sufficiente (20s sembra adeguato)

3. **Dependency ordering robusta in docker-compose**:
   - `seeder`: `depends_on: db: condition: service_healthy`
   - `backend`: `depends_on: db: condition: service_healthy seeder: condition: service_completed_successfully`
   - `frontend`: `depends_on: backend: condition: service_started`
   - NOTA: `depends_on` non aspetta che il backend sia HEALTHY, solo STARTED. Valutare se serve wait-for-it nel frontend.

4. **Wait-for-it pattern per il frontend (opzionale)**:
   - Se il frontend parte prima che il backend sia pronto, le chiamate API falliranno
   - Soluzione 1: Aggiungere retry lato JavaScript (più resiliente)
   - Soluzione 2: Script di attesa nel frontend Dockerfile (più deterministico)
   - **Scelta consigliata**: Retry lato JS (già implementato parzialmente in `api.js`)

5. **Retry policy nel seeder**:
   - FilmApiSeeder tenta la connessione al DB una volta sola
   - Aggiungere retry con backoff per gestire il caso in cui MariaDB sia appena diventato healthy ma non accetti ancora connessioni
   - Usare `Pomelo.EntityFrameworkCore.MySql` connection retry policy

6. **Timeout e startup sequence**:
   - Documentare i tempi di attesa previsti:
     - MariaDB: ~20s per healthcheck iniziale
     - Seeder: ~2-5 min (TMDB API calls)
     - Backend: ~10s dopo seeder completato
     - Frontend: immediato
   - **Tempo totale stimato**: ~3-8 minuti al primo avvio

**File da creare/modificare**:
- `backend/FilmAPI/Program.cs` — MODIFICARE: aggiungere endpoint `GET /health`
- `backend/FilmAPI/Endpoints/` — CREARE: `HealthEndpoints.cs` (o aggiungere inline in Program.cs)
- `docker-compose.yml` — MODIFICARE: healthcheck per backend e frontend
- `backend/scripts/FilmApiSeeder/Program.cs` — MODIFICARE: retry connessione DB

**Criteri di accettazione fase**:
1. `docker-compose up -d` completa senza errori
2. Dopo l'avvio, `curl http://localhost:5000/health` restituisce 200
3. Se il DB è lento ad avviarsi, backend e seeder aspettano pazientemente
4. Se il seeder fallisce, il backend non parte (dipende dal seeder completato con successo)
5. Il frontend serve pagine anche se il backend non è ancora pronto (ma le chiamate API falliscono con retry)

**Test fase**:
- `docker-compose up -d` → attesa → `docker-compose ps` → tutti `Up` o `Completed`
- `docker-compose logs backend` → mostra "Seeding completato" e "Application started"
- `curl http://localhost:5000/health` → `{"status":"Healthy","database":"Connected"}`
- Test resilienza: kill db container → backend dovrebbe fallire healthcheck

---

### FASE 4 — ACA Infrastructure as Code (IaC)

**Obiettivo**: Creare/aggiornare gli script per il provisioning automatico delle risorse Azure necessarie per CineBase su ACA.

**Attività**:

1. **Review e validazione `infra/azure/aca-deploy.ps1`**:
   - Testare lo script esistente con una subscription Azure reale
   - Identificare eventuali problemi di sintassi o API deprecate
   - Verificare che la sintassi `--set` per i volumi Azure Files funzioni con l'ultima versione dell'estensione ACA

2. **Refinement dello script di deploy**:
   - Correggere eventuali bug trovati nella validazione
   - Migliorare la gestione degli errori (try/catch, rollback)
   - Aggiungere parametro `-WhatIf` per dry-run
   - Aggiungere logging strutturato
   - Estrarre le configurazioni sensibili in variabili con prompt interattivo

3. **Creare template Bicep/ARM (opzionale ma consigliato)**:
   - Bicep permette dichiarazione IaC più strutturata degli script shell
   - Creare `infra/azure/main.bicep` con tutti i resource provider:
     - Resource Group
     - Container Registry (ACR)
     - Log Analytics Workspace
     - Container Apps Environment
     - Storage Account + Azure Files shares
     - Container Apps (db, api, web)
     - Container App Job (seeder)
   - Vantaggio: idempotente, dichiarativo, supporta CI/CD

4. **GitHub Actions workflow per deploy automatico** (opzionale):
   - Creare `.github/workflows/deploy-aca.yml`
   - Build immagini su push su branch `main`
   - Push su ACR
   - Deploy su ACA
   - Esecuzione seeder

5. **Documentazione parametri deploy**:
   - Creare tabella riassuntiva di tutti i parametri richiesti
   - Costi stimati per environment (dev/prod)

**File da creare/modificare**:
- `infra/azure/aca-deploy.ps1` — MODIFICARE: refinement, error handling, parametri
- `infra/azure/main.bicep` — CREARE (opzionale): template Bicep per tutte le risorse
- `infra/azure/parameters.json` — CREARE (opzionale): parametri Bicep
- `infra/azure/README.md` — CREARE: documentazione uso script
- `.github/workflows/deploy-aca.yml` — CREARE (opzionale): CI/CD automatico

**Criteri di accettazione fase**:
1. `aca-deploy.ps1` si esegue senza errori (test con subscription Azure)
2. Tutte le risorse Azure vengono create correttamente
3. Lo script gestisce casi di errore (risorse già esistenti, permessi insufficienti)
4. I segreti non sono in chiaro nei log

**Test fase**:
- `.\infra\azure\aca-deploy.ps1 -WhatIf` → mostra azioni previste senza eseguire
- `az deployment group validate` per template Bicep (se creato)
- Verifica manuale su subscription Azure for Students (costo zero con ACR Basic)

---

### FASE 5 — Build e Push Immagini su ACR

**Obiettivo**: Preparare le immagini Docker e caricarle su Azure Container Registry per il deploy su ACA.

**Attività**:

1. **Login ad ACR**:
   ```powershell
   az acr login --name $ACR_NAME
   ```

2. **Build immagini** (Opzione A: Docker locale):
   ```powershell
   # Backend API
   docker build -t cinebase-api -f backend/Dockerfile backend
   docker tag cinebase-api $ACR_LOGIN_SERVER/cinebase-api:latest
   docker tag cinebase-api $ACR_LOGIN_SERVER/cinebase-api:1.0.0
   docker push $ACR_LOGIN_SERVER/cinebase-api:latest
   docker push $ACR_LOGIN_SERVER/cinebase-api:1.0.0

   # Frontend Web
   docker build -t cinebase-web -f frontend/CineBase.Web/Dockerfile frontend/CineBase.Web
   docker tag cinebase-web $ACR_LOGIN_SERVER/cinebase-web:latest
   docker tag cinebase-web $ACR_LOGIN_SERVER/cinebase-web:1.0.0
   docker push $ACR_LOGIN_SERVER/cinebase-web:latest
   docker push $ACR_LOGIN_SERVER/cinebase-web:1.0.0

   # Seeder
   docker build -t cinebase-seeder -f backend/scripts/FilmApiSeeder/Dockerfile backend
   docker tag cinebase-seeder $ACR_LOGIN_SERVER/cinebase-seeder:latest
   docker tag cinebase-seeder $ACR_LOGIN_SERVER/cinebase-seeder:1.0.0
   docker push $ACR_LOGIN_SERVER/cinebase-seeder:latest
   docker push $ACR_LOGIN_SERVER/cinebase-seeder:1.0.0
   ```

3. **Build su ACR** (Opzione B: senza Docker locale):
   ```powershell
   az acr build --registry $ACR_NAME --image cinebase-api:latest --file backend/Dockerfile ./backend
   az acr build --registry $ACR_NAME --image cinebase-web:latest --file frontend/CineBase.Web/Dockerfile ./frontend/CineBase.Web
   az acr build --registry $ACR_NAME --image cinebase-seeder:latest --file backend/scripts/FilmApiSeeder/Dockerfile ./backend
   ```

4. **Multi-arch build** (opzionale, per ARM64 come Apple Silicon):
   ```powershell
   docker buildx build --platform linux/amd64,linux/arm64 -t $ACR_LOGIN_SERVER/cinebase-api:latest --push -f backend/Dockerfile backend
   ```

5. **Verifica immagini su ACR**:
   ```powershell
   az acr repository list --name $ACR_NAME --output table
   az acr repository show-tags --name $ACR_NAME --repository cinebase-api --output table
   ```

**File da creare/modificare**:
- Nessun file — operazioni su ACR

**Criteri di accettazione fase**:
1. Le 3 immagini sono presenti in ACR con tag `latest` e `1.0.0`
2. Le immagini possono essere runnate localmente da ACR
3. `docker run $ACR_LOGIN_SERVER/cinebase-api:latest` funziona

**Test fase**:
- `az acr repository list` → 3 repository
- `docker run $ACR_LOGIN_SERVER/cinebase-api:latest` → app parte
- Tag presenti: `latest`, `1.0.0`

---

### FASE 6 — Deploy MariaDB su ACA con Azure Files

**Obiettivo**: Deployare MariaDB su ACA con storage persistente su Azure Files.

**Attività**:

1. **Creare Storage Account e File Share** (già in script):
   ```powershell
   az storage account create --name $STORAGE_NAME --resource-group $RG --location $LOC --sku Standard_LRS
   az storage share create --name "mariadb-data" --account-name $STORAGE_NAME --quota 5
   ```

2. **Creare Container App per MariaDB**:
   ```powershell
   az containerapp create --name "cinebase-db" --resource-group $RG --environment $ACA_ENV `
     --image mariadb:11.4 `
     --min-replicas 0 --max-replicas 1 `
     --secrets "mariadb-root-password=$DB_PASSWORD" "azure-storage-account-key=$STORAGE_KEY" `
     --env-vars "MARIADB_ROOT_PASSWORD=secretref:mariadb-root-password" "MARIADB_DATABASE=film-api-db" `
     --target-port 3306 --ingress internal `
     --cpu 0.5 --memory 1Gi
   ```

3. **Montare Azure Files Volume**:
   ```powershell
   # Aggiungere volume Azure Files per persistenza
   az containerapp update --name "cinebase-db" --resource-group $RG `
     --set "template.volumes=[{\"name\":\"mariadb-data\",\"storageName\":\"mariadb-data\",\"storageType\":\"AzureFile\"}]" `
     --set "template.containers[0].volumeMounts=[{\"volumeName\":\"mariadb-data\",\"mountPath\":\"/var/lib/mysql\"}]"
   ```

4. **Verifica**:
   - Controllare che l'app container sia in running state
   - Testare connessione dal backend (passo successivo)

**File da creare/modificare**:
- Già coperto dallo script `aca-deploy.ps1` — validare e correggere se necessario

**Criteri di accettazione fase**:
1. MariaDB container è in stato `Running`
2. Il volume è montato correttamente (`/var/lib/mysql`)
3. I dati persistono dopo riavvio del container (test: scrivi dato, riavvia, verifica)
4. L'ingress è interno (non accessibile da Internet)

**Test fase**:
- `az containerapp show --name cinebase-db --query properties.runningStatus`
- `az containerapp logs show --name cinebase-db --follow` → MariaDB ready for connections
- Test persistenza: connetti, crea tabella, riavvia container, verifica tabella esiste

---

### FASE 7 — Deploy Backend API su ACA

**Obiettivo**: Deployare il backend API CineBase su ACA con ingress interno e secrets gestiti.

**Attività**:

1. **Preparare secrets ACA per il backend**:
   ```
   mariadb-root-password
   azure-storage-account-key
   jwt-secret
   tmdb-bearer-token
   stripe-secret-key (opzionale)
   stripe-webhook-secret (opzionale)
   smtp-password (opzionale)
   auth-google-clientid (opzionale)
   auth-google-clientsecret (opzionale)
   auth-microsoft-clientid (opzionale)
   auth-microsoft-clientsecret (opzionale)
   ```

2. **Creare Container App per Backend**:
   ```powershell
   az containerapp create --name "cinebase-api" --resource-group $RG --environment $ACA_ENV `
     --image $ACR_LOGIN_SERVER/cinebase-api:latest `
     --registry-server $ACR_LOGIN_SERVER `
     --registry-username $ACR_USER --registry-password $ACR_PASS `
     --min-replicas 0 --max-replicas 3 `
     --secrets "mariadb-root-password=$DB_PASS" "jwt-secret=$JWT_SECRET" "tmdb-bearer-token=$TMDB_TOKEN" `
     --env-vars "ASPNETCORE_ENVIRONMENT=Production" "ASPNETCORE_URLS=http://+:8080" `
                "DB_HOST=cinebase-db" "DB_PORT=3306" "DB_NAME=film-api-db" "DB_USER=root" `
                "DB_PASSWORD=secretref:mariadb-root-password" `
                "JWT_SECRET=secretref:jwt-secret" "JWT_ISSUER=CineBaseAPI" "JWT_AUDIENCE=CineBaseWeb" `
                "TMDB_BEARER_TOKEN=secretref:tmdb-bearer-token" `
                "ADMIN_SEED_EMAIL=admin@cinebase.it" "ADMIN_SEED_PASSWORD=Admin123!" `
                "ALLOWED_CORS_ORIGINS=https://<FRONTEND_URL>,http://localhost:5001" `
                "FRONTEND_BASE_URL=https://<FRONTEND_URL>" `
     --target-port 8080 --ingress internal `
     --cpu 0.5 --memory 1Gi
   ```

3. **Configurazione SMTP sul backend ACA**:
   - Aggiungere env vars per SMTP se configurato
   - Usare secretref per SMTP password

4. **Configurazione OAuth sul backend ACA**:
   - Aggiungere env vars per Google/Microsoft OAuth
   - Usare secretref per ClientId/ClientSecret

5. **Health probe per ACA**:
   - ACA usa health probe per determinare se un'istanza è sana
   - Configurare `livenessProbe` e `readinessProbe` sull'endpoint `/health`
   ```powershell
   az containerapp update --name "cinebase-api" --resource-group $RG `
     --set "template.containers[0].livenessProbe={\"path\":\"/health\",\"port\":8080,\"type\":\"http\"}" `
     --set "template.containers[0].readinessProbe={\"path\":\"/health\",\"port\":8080,\"type\":\"http\"}"
   ```

**File da creare/modificare**:
- `infra/azure/aca-deploy.ps1` — MODIFICARE: sezioni per secrets, health probe
- `infra/azure/ACA-DEPLOY-GUIDE.md` — MODIFICARE: allineare con la procedura effettiva

**Criteri di accettazione fase**:
1. Backend container è in stato `Running`
2. Health check `/health` risponde 200
3. Il backend può connettersi a MariaDB
4. L'ingress è interno (raggiungibile solo dalla rete ACA)
5. CORS è configurato correttamente per il frontend

**Test fase**:
- `az containerapp show --name cinebase-api --query properties.runningStatus`
- `az containerapp logs show --name cinebase-api --follow` → "Application started"
- Da un container nella stessa ACA env: `curl http://cinebase-api:8080/health`

---

### FASE 8 — Deploy Seeder come ACA Job

**Obiettivo**: Deployare il FilmApiSeeder come ACA Job one-shot che popola il database.

**Attività**:

1. **Creare ACA Job per il seeder**:
   ```powershell
   az containerapp job create --name "cinebase-seeder" --resource-group $RG --environment $ACA_ENV `
     --image $ACR_LOGIN_SERVER/cinebase-seeder:latest `
     --registry-server $ACR_LOGIN_SERVER `
     --registry-username $ACR_USER --registry-password $ACR_PASS `
     --trigger-type Manual `
     --secrets "mariadb-root-password=$DB_PASS" "tmdb-bearer-token=$TMDB_TOKEN" `
     --env-vars "ASPNETCORE_ENVIRONMENT=Production" `
                "DB_HOST=cinebase-db" "DB_PORT=3306" "DB_NAME=film-api-db" "DB_USER=root" `
                "DB_PASSWORD=secretref:mariadb-root-password" `
                "TMDB_BEARER_TOKEN=secretref:tmdb-bearer-token" `
                "ADMIN_SEED_EMAIL=admin@cinebase.it" "ADMIN_SEED_PASSWORD=Admin123!" `
     --cpu 0.5 --memory 1Gi
   ```

2. **Eseguire il seeder**:
   ```powershell
   az containerapp job start --name "cinebase-seeder" --resource-group $RG
   ```

3. **Verificare esecuzione**:
   ```powershell
   az containerapp job execution list --name "cinebase-seeder" --resource-group $RG --output table
   ```

4. **Opzioni aggiuntive**:
   - Aggiungere flag `--reset-shows --force` per rigenerare la programmazione se necessario
   - Il seeder può essere rieseguito on-demand per aggiornare i film

**File da creare/modificare**:
- `infra/azure/aca-deploy.ps1` — MODIFICARE: sezione seeder job
- `infra/azure/ACA-DEPLOY-GUIDE.md` — MODIFICARE: documentare esecuzione seeder

**Criteri di accettazione fase**:
1. ACA Job esiste con trigger manuale
2. L'esecuzione del job completa con exit code 0
3. I log mostrano `Completato: N film, M cinema, S sale`
4. I dati sono accessibili dal backend

**Test fase**:
- Job execution: `az containerapp job start --name cinebase-seeder -g $RG`
- Verifica exit code: `az containerapp job execution list --name cinebase-seeder -g $RG -o table`
- Verifica log: mostra conteggio film, cinema, sale
- Verifica API: chiamata a backend per `GET /programmazione/films` ritorna dati

---

### FASE 9 — Deploy Frontend su ACA con Data Protection

**Obiettivo**: Deployare il frontend su ACA con ingress esterno e supporto per scaling orizzontale (Data Protection Keys condivise).

**Attività**:

1. **Creare File Share per Data Protection Keys** (se non già fatto):
   ```powershell
   az storage share create --name "web-dataprotection-keys" --account-name $STORAGE_NAME --quota 1
   ```

2. **Creare Container App per Frontend**:
   ```powershell
   az containerapp create --name "cinebase-web" --resource-group $RG --environment $ACA_ENV `
     --image $ACR_LOGIN_SERVER/cinebase-web:latest `
     --registry-server $ACR_LOGIN_SERVER `
     --registry-username $ACR_USER --registry-password $ACR_PASS `
     --min-replicas 0 --max-replicas 3 `
     --secrets "azure-storage-account-key=$STORAGE_KEY" `
     --env-vars "ASPNETCORE_ENVIRONMENT=Production" `
                "ASPNETCORE_URLS=http://+:8080" `
                "BACKEND_API_URL=http://cinebase-api:8080" `
     --target-port 8080 --ingress external `
     --enable-session-affinity `
     --cpu 0.5 --memory 1Gi
   ```

3. **Montare volume Azure Files per Data Protection Keys**:
   ```powershell
   az containerapp update --name "cinebase-web" --resource-group $RG `
     --set "template.volumes=[{\"name\":\"dataprotection\",\"storageName\":\"web-dataprotection-keys\",\"storageType\":\"AzureFile\"}]" `
     --set "template.containers[0].volumeMounts=[{\"volumeName\":\"dataprotection\",\"mountPath\":\"/mnt/dataprotectionkeys\"}]"
   ```

4. **Aggiornare Data Protection in `Program.cs` del frontend**:
   - Attualmente il frontend NON configura Data Protection
   - Aggiungere configurazione per leggere `DATA_PROTECTION_KEYS_PATH` da env var
   - Se la variabile è impostata, usare il filesystem persistente per le chiavi

   ```csharp
   // Aggiungere in Program.cs dopo builder.Build()
   var dataProtectionKeysPath = Environment.GetEnvironmentVariable("DATA_PROTECTION_KEYS_PATH");
   if (!string.IsNullOrEmpty(dataProtectionKeysPath))
   {
       builder.Services.AddDataProtection()
           .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
           .SetApplicationName("CineBase");
   }
   ```

   **NOTA**: Questa modifica richiede di capire se il frontend usa Data Protection (cookie auth). Attualmente il frontend è un'app statica che NON usa cookie auth, quindi le Data Protection Keys potrebbero non essere necessarie. Verificare prima di implementare.

5. **Ottenere URL pubblico**:
   ```powershell
   $WEB_URL = az containerapp show --name cinebase-web -g $RG --query properties.configuration.ingress.fqdn --output tsv
   Write-Output "Frontend: https://$WEB_URL"
   ```

6. **Aggiornare backend con URL frontend corretto**:
   ```powershell
   az containerapp update --name cinebase-api -g $RG `
     --set "environmentVariables[?name=='ALLOWED_CORS_ORIGINS'].value=https://$WEB_URL,http://localhost:5001" `
     --set "environmentVariables[?name=='FRONTEND_BASE_URL'].value=https://$WEB_URL"
   ```

**File da creare/modificare**:
- `frontend/CineBase.Web/Program.cs` — MODIFICARE: aggiungere Data Protection condizionale (se serve)
- `infra/azure/aca-deploy.ps1` — MODIFICARE: sezione frontend
- `infra/azure/ACA-DEPLOY-GUIDE.md` — MODIFICARE: documentare deploy frontend

**Criteri di accettazione fase**:
1. Frontend container è in stato `Running` con ingress esterno
2. URL pubblico `https://<name>.italynorth.azurecontainerapps.io` è raggiungibile
3. Le pagine HTML vengono servite correttamente
4. Le chiamate API dal frontend al backend funzionano (middleware injecta `BACKEND_API_URL`)
5. Session affinity è abilitata

**Test fase**:
- `curl https://$WEB_URL/index.html` → 200, HTML corretto
- Verifica `API_BASE_URL` nel sorgente HTML: `curl https://$WEB_URL/index.html | grep API_BASE_URL`
- Verifica chiamata API: accedi a una pagina che fa chiamate API (es. programmazione)
- Test login/registrazione end-to-end

---

### FASE 10 — Configurazione OAuth, SMTP, Dominio Personalizzato

**Obiettivo**: Configurare i servizi esterni (OAuth, SMTP) e opzionalmente il dominio personalizzato per ACA.

**Attività**:

1. **Aggiornare Redirect URI OAuth**:
   - Dopo il deploy, aggiungere l'URL ACA ai redirect URI nelle console dei provider:

   **Google Cloud Console**:
   ```
   URI: https://<FRONTEND_URL>/signin-google
   ```

   **Microsoft Entra ID**:
   ```
   URI: https://<FRONTEND_URL>/signin-microsoft
   ```

2. **Aggiornare env var OAuth sul backend ACA**:
   ```powershell
   az containerapp update --name cinebase-api -g $RG `
     --set "secrets[?name=='auth-google-clientid'].value=<GOOGLE_CLIENT_ID>" `
     --set "secrets[?name=='auth-google-clientsecret'].value=<GOOGLE_CLIENT_SECRET>" `
     --set "secrets[?name=='auth-microsoft-clientid'].value=<MICROSOFT_CLIENT_ID>" `
     --set "secrets[?name=='auth-microsoft-clientsecret'].value=<MICROSOFT_CLIENT_SECRET>"
   ```

3. **Configurazione Dominio Personalizzato (opzionale)**:
   - Aggiungere record CNAME e TXT nel DNS del dominio
   - Configurare certificato gestito da ACA (gratuito, rinnovo automatico)
   - Verificare che HTTPS funzioni con il dominio personalizzato

4. **Configurazione SMTP per email reali**:
   - Se si usa Gmail SMTP: configurare `SMTP_*` env var sul backend
   - Se si usa Twilio SendGrid: configurare con API key
   - Le credenziali SMTP vanno come secrets ACA

**File da creare/modificare**:
- `infra/azure/ACA-DEPLOY-GUIDE.md` — MODIFICARE: sezione OAuth/SMTP/dominio

**Criteri di accettazione fase**:
1. Login con Google OAuth funziona su ACA
2. Login con Microsoft OAuth funziona su ACA (con account `@issgreppi.it`)
3. Le email inviate dall'app arrivano realmente (se SMTP configurato)
4. (Opzionale) Il dominio personalizzato è raggiungibile via HTTPS

**Test fase**:
- Test Google OAuth: click "Continua con Google" → redirect → login OK
- Test Microsoft OAuth: click "Continua con Microsoft" → login OK
- Test forgot password: richiedi reset → email ricevuta
- Test acquisto: email biglietto ricevuta

---

### FASE 11 — Test End-to-End, Verifica e Documentazione

**Obiettivo**: Verificare che tutto funzioni end-to-end sia in locale (docker-compose) che su ACA, e aggiornare la documentazione.

**Attività**:

1. **Test End-to-End Locale**:
   ```bash
   # 1. Clean clone simulation
   git clone <repo> cinebase-test
   cd cinebase-test
   cp .env.example .env
   docker-compose up -d
   
   # 2. Wait for startup
   docker-compose logs -f (watch for seeder completion)
   
   # 3. Run test suite
   dotnet test tests/backend/FilmAPI.Tests.csproj
   
   # 4. Smoke test frontend
   curl http://localhost:5001/index.html
   curl http://localhost:5001/programmazione.html
   
   # 5. Smoke test backend
   curl http://localhost:5000/health
   curl http://localhost:5000/programmazione/films
   
   # 6. Test login admin
   curl -X POST http://localhost:5000/auth/login -H "Content-Type: application/json" -d '{"email":"admin@cinebase.it","password":"Admin123!"}'
   
   # 7. Test MailDev
   curl http://localhost:1080
   ```

2. **Test End-to-End ACA**:
   ```powershell
   # 1. Smoke test frontend pubblico
   curl https://$WEB_URL/index.html
   
   # 2. Smoke test backend (via frontend proxy o direct test)
   curl http://cinebase-api:8080/health (da jumpbox in ACA)
   
   # 3. Test login admin
   curl -X POST http://cinebase-api:8080/auth/login -H "Content-Type: application/json" -d '{"email":"admin@cinebase.it","password":"Admin123!"}'
   
   # 4. Test programmazione
   curl http://cinebase-api:8080/programmazione/films
   
   # 5. Test seeder logs
   az containerapp job execution list --name cinebase-seeder -g $RG -o table
   ```

3. **Aggiornare `docs/project/status.md`**:
   - Aggiungere stato Iterazione 6 con tabella fasi
   - Indicare ACA URL, ACR name, Resource Group
   - Aggiornare numero test backend

4. **Aggiornare `docs/project/changelog.md`**:
   - Dockerfile multistage ottimizzati
   - docker-compose con MailDev e configurazione completa
   - ACA Infrastructure as Code
   - Deploy ACA completato

5. **Creare `infra/azure/ACA-DEPLOY-GUIDE.md` finale**:
   - Documento autonomo con tutti i passi per deployare CineBase su ACA
   - Requisiti, prerequisiti, procedura passo-passo
   - Troubleshooting comune
   - Stima costi

**File da creare/modificare**:
- `docs/project/status.md` — MODIFICARE
- `docs/project/changelog.md` — MODIFICARE
- `docs/project/dev_iteration/6/PianoDiLavoro.md` — MODIFICARE (completare stato fasi)
- `infra/azure/ACA-DEPLOY-GUIDE.md` — MODIFICARE (versione finale)

**Criteri di accettazione fase**:
1. Tutti i test backend passano (sia in locale che in container)
2. docker-compose locale funziona senza errori
3. L'app ACA è raggiungibile e funzionante
4. Tutta la documentazione è aggiornata

**Test fase**:
- Suite backend completa
- Smoke test frontend (pagine raggiungibili, login funziona)
- ACA accessibility test (URL pubblico raggiungibile, API rispondono)

---

## 6) File e Aree Impattate

### 6.1 Docker / Container

| File | Azione | Descrizione |
|------|--------|-------------|
| `docker-compose.yml` | MODIFICARE | Aggiungere maildev, variabili mancanti, restart policy, healthcheck |
| `backend/Dockerfile` | MODIFICARE | Labels, USER app, HEALTHCHECK |
| `frontend/CineBase.Web/Dockerfile` | MODIFICARE | Labels, USER app, HEALTHCHECK, FORWARDEDHEADERS |
| `backend/scripts/FilmApiSeeder/Dockerfile` | MODIFICARE | Passare a `runtime:9.0`, labels |
| `.env.example` | MODIFICARE | Variabili complete commentate |
| `backend/.env.example` | MODIFICARE | Allineato con letture effettive |

### 6.2 Backend (`backend/FilmAPI/`)

| File | Azione | Descrizione |
|------|--------|-------------|
| `Program.cs` | MODIFICARE | Aggiungere endpoint `GET /health` |
| `FilmAPI.csproj` | NESSUNA | Già include tutte le dipendenze necessarie |

### 6.3 Frontend (`frontend/CineBase.Web/`)

| File | Azione | Descrizione |
|------|--------|-------------|
| `Program.cs` | MODIFICARE (da valutare) | Data Protection Keys da env var se serve |

### 6.4 Infrastruttura Azure (`infra/azure/`)

| File | Azione | Descrizione |
|------|--------|-------------|
| `aca-deploy.ps1` | MODIFICARE | Refinement, error handling, secrets, data protection volume |
| `ACA-DEPLOY-GUIDE.md` | MODIFICARE | Versione finale della guida di deploy |
| `main.bicep` | CREARE (opzionale) | Template Bicep per IaC dichiarativa |
| `parameters.json` | CREARE (opzionale) | Parametri per template Bicep |
| `README.md` | CREARE | Documentazione script e parametri |

### 6.5 CI/CD (opzionale)

| File | Azione | Descrizione |
|------|--------|-------------|
| `.github/workflows/deploy-aca.yml` | CREARE (opzionale) | Build automatica su push e deploy ACA |

### 6.6 Documentazione

| File | Azione | Descrizione |
|------|--------|-------------|
| `docs/project/status.md` | MODIFICARE | Stato Iterazione 6 |
| `docs/project/changelog.md` | MODIFICARE | Changelog Iterazione 6 |
| `docs/project/dev_iteration/6/PianoDiLavoro.md` | MODIFICARE | Aggiornamento stato fasi al completamento |
| `README.md` (radice) | CREARE (opzionale) | Quick start con docker-compose |

---

## 7) Mappa Variabili d'Ambiente

### 7.1 Variabili Backend (lette da `Program.cs`)

| Variabile | Default | Obbligatoria | Dove si usa |
|-----------|---------|-------------|-------------|
| `DB_HOST` | `localhost` | Sì | Connessione MariaDB |
| `DB_PORT` | `3306` | Sì | Connessione MariaDB |
| `DB_NAME` | `film-api-db` | Sì | Connessione MariaDB |
| `DB_USER` | `root` | Sì | Connessione MariaDB |
| `DB_PASSWORD` | `root` | Sì | Connessione MariaDB |
| `JWT_SECRET` | - | Sì | Firma JWT (min 256 bit) |
| `JWT_ISSUER` | `CineBaseAPI` | No | Issuer JWT |
| `JWT_AUDIENCE` | `CineBaseWeb` | No | Audience JWT |
| `ADMIN_SEED_EMAIL` | `admin@cinebase.it` | No | Email admin iniziale |
| `ADMIN_SEED_PASSWORD` | `Admin123!` | No | Password admin iniziale |
| `ALLOWED_CORS_ORIGINS` | `http://localhost:5001` | Sì | URL frontend per CORS |
| `FRONTEND_BASE_URL` | `http://localhost:5001` | Sì | URL base frontend (redirect OAuth) |
| `SMTP_HOST` | - | No | Host server SMTP |
| `SMTP_PORT` | `587` | No | Porta SMTP |
| `SMTP_USER` | - | No | Username SMTP |
| `SMTP_PASSWORD` | - | No | Password SMTP |
| `SMTP_FROM_EMAIL` | - | No | Mittente email |
| `SMTP_FROM_NAME` | - | No | Nome mittente |
| `GOOGLE_CLIENT_ID` | - | No | Google OAuth Client ID |
| `GOOGLE_CLIENT_SECRET` | - | No | Google OAuth Client Secret |
| `MICROSOFT_CLIENT_ID` | - | No | Microsoft OAuth Client ID |
| `MICROSOFT_CLIENT_SECRET` | - | No | Microsoft OAuth Client Secret |
| `MICROSOFT_TENANT_ID` | `organizations` | No | Tenant Microsoft |
| `STRIPE_SECRET_API_KEY` | - | No | Stripe API key |
| `STRIPE_WEBHOOK_SECRET` | - | No | Stripe webhook secret |
| `TMDB_BEARER_TOKEN` | - | Sì (per seeder) | Token API TMDB |

### 7.2 Variabili Frontend

| Variabile | Default | Obbligatoria | Dove si usa |
|-----------|---------|-------------|-------------|
| `BACKEND_API_URL` | `http://localhost:5000` | Sì | URL backend (iniettato nel middleware) |

### 7.3 Secrets ACA

| Segreto ACA | Ref nelle env var | Descrizione |
|-------------|-------------------|-------------|
| `mariadb-root-password` | `secretref:mariadb-root-password` | Password root MariaDB |
| `azure-storage-account-key` | `secretref:azure-storage-account-key` | Chiave storage Azure Files |
| `jwt-secret` | `secretref:jwt-secret` | Chiave firma JWT |
| `tmdb-bearer-token` | `secretref:tmdb-bearer-token` | Token TMDB per seeder |
| `stripe-secret-api-key` | `secretref:stripe-secret-api-key` | Stripe API key (opzionale) |
| `stripe-webhook-secret` | `secretref:stripe-webhook-secret` | Stripe webhook secret (opzionale) |
| `smtp-password` | `secretref:smtp-password` | Password SMTP (opzionale) |
| `auth-google-clientid` | `secretref:auth-google-clientid` | Google Client ID (opzionale) |
| `auth-google-clientsecret` | `secretref:auth-google-clientsecret` | Google Client Secret (opzionale) |
| `auth-microsoft-clientid` | `secretref:auth-microsoft-clientid` | Microsoft Client ID (opzionale) |
| `auth-microsoft-clientsecret` | `secretref:auth-microsoft-clientsecret` | Microsoft Client Secret (opzionale) |

---

## 8) Criteri di Accettazione Finali

L'Iterazione 6 può essere marcata completata solo se:

1. **CA1**: `docker-compose up -d` produce un'app funzionante con DB, backend, frontend, seeder.
2. **CA2**: Il seeder popola automaticamente film, cinema, sale e show da TMDB.
3. **CA3**: L'account `admin@cinebase.it` / `Admin123!` è funzionante al primo avvio.
4. **CA4**: MailDev è accessibile su `http://localhost:1080` e intercetta le email.
5. **CA5**: `docker-compose down` e `docker-compose up -d` preservano i dati del database (volume persistente).
6. **CA6**: I Dockerfile sono multistage ottimizzati con labels, USER app e HEALTHCHECK.
7. **CA7**: Lo script `aca-deploy.ps1` crea tutte le risorse Azure e deploya l'app senza errori.
8. **CA8**: L'app su ACA è raggiungibile pubblicamente via URL ACA.
9. **CA9**: Le chiamate API dal frontend ACA al backend ACA funzionano (internal ingress).
10. **CA10**: OAuth (Google/Microsoft) funziona su ACA se configurato.
11. **CA11**: Il seeder può essere rieseguito su ACA come job on-demand.
12. **CA12**: I test backend passano (suite completa verde).
13. **CA13**: `status.md` e `changelog.md` sono aggiornati.
14. **CA14**: La guida ACA-DEPLOY-GUIDE.md è completa e verificata.

---

## 9) Rischio e Mitigazioni

| Rischio | Probabilità | Impatto | Mitigazione |
|---------|-------------|---------|-------------|
| ACA extension sintassi `--set` per volumi cambia tra versioni | Media | Alto | Testare su subscription Azure reale prima del deploy finale |
| Azure for Students non supporta SKU Basic ACR in alcune regioni | Media | Alto | Usare `westeurope` come fallback, documentare limitazioni |
| FilmApiSeeder timeout su TMDB per troppe richieste | Alta | Medio | Implementare retry con backoff, documentare limite rate TMDB |
| docker-compose non funziona su Windows (path mapping, permessi) | Media | Medio | Testare su Windows, usare path relativi con `.` |
| Frontend Data Protection non necessaria (nessun cookie auth) | Media | Basso | Verificare prima di implementare; skip se non serve |
| ACA cold start lento (min-replicas=0) | Alta | Basso | Documentare che il primo accesso può essere lento |
| Secrets ACA limite di caratteri | Bassa | Medio | Mantenere secrets concisi, usare env var per valori non sensibili |
| Multi-arch build (ARM64 vs AMD64) | Media | Medio | Usare `docker buildx` per build multi-arch o buildare su ACR |

---

## 10) Stima Effort

| Attività | Tempo stimato |
|----------|---------------|
| FASE 1 — Refinement Dockerfile e docker-compose | 60-90 min |
| FASE 2 — Configurazione "Clone & Run" | 30-60 min |
| FASE 3 — Healthcheck e startup robusta | 60-90 min |
| FASE 4 — ACA Infrastructure as Code | 90-180 min |
| FASE 5 — Build e Push immagini su ACR | 30-60 min |
| FASE 6 — Deploy MariaDB su ACA | 30-45 min |
| FASE 7 — Deploy Backend API su ACA | 45-60 min |
| FASE 8 — Deploy Seeder ACA Job | 20-30 min |
| FASE 9 — Deploy Frontend su ACA | 45-60 min |
| FASE 10 — Configurazione OAuth, SMTP, Dominio | 60-120 min |
| FASE 11 — Test E2E, verifica e documentazione | 60-120 min |
| **Totale realistico** | **~10-16 ore** (2-3 giornate) |

---

## 11) Riferimenti

- **Guida ACA EducationalGames**: https://github.com/GreppiDev/Info5IA2526WebDev/blob/main/azure/containers/examples/educationalgames/aca/index.md
- **Azure Container Apps documentation**: https://learn.microsoft.com/en-us/azure/container-apps/
- **Azure CLI Container Apps extension**: `az extension add --name containerapp`
- **MariaDB Docker image**: https://hub.docker.com/_/mariadb
- **MailDev**: https://github.com/maildev/maildev
- **Iterazione 5 piano**: `docs/project/dev_iteration/5/5.md` (struttura di riferimento)
- **Guida ACA CineBase esistente**: `infra/azure/ACA-DEPLOY-GUIDE.md`
- **Script deploy esistente**: `infra/azure/aca-deploy.ps1`
