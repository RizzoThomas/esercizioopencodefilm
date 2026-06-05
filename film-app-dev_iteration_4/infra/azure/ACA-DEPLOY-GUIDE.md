# Guida Passo Passo - Deploy CineBase su Azure Container Apps (ACA)

Basata sulla guida di riferimento: [EducationalGames ACA Deployment](https://github.com/GreppiDev/Info5IA2526WebDev/blob/main/azure/containers/examples/educationalgames/aca/index.md)

## Architettura CineBase su ACA

```
Frontend (ext) ──> Backend API (int) ──> MariaDB (int)
                        │
                   Seeder Job (ACA Job, one-shot)
```

- **Frontend**: External ingress (pubblico), servito da ASP.NET Core (file statici)
- **Backend API**: Internal ingress (solo rete interna ACA), API REST .NET 9
- **MariaDB**: Internal ingress, storage persistente su Azure Files
- **Seeder**: ACA Job one-shot, popola film da TMDB

## Prerequisiti

- **Account Azure** con sottoscrizione attiva
- **Azure CLI** installata e autenticata (`az login`)
- **Docker Desktop** (opzionale - si può usare `az acr build` senza Docker locale)
- **TMDB Bearer Token** (da https://www.themoviedb.org/settings/api)
- **Codice già patchato per ACA** (applicato in questa iterazione):
  - `backend/FilmAPI/Program.cs`: CORS legge `ALLOWED_CORS_ORIGINS` da env var
  - `frontend/CineBase.Web/Program.cs`: middleware injecta `BACKEND_API_URL` nell'HTML

---

## Passo 1: Creare un Azure Container Registry (ACR)

```powershell
# Variabili - personalizza questi valori
$RESOURCE_GROUP="rg-cinebase-prod"
$LOCATION="italynorth"
$ACR_NAME="acrcinebase$(-join ((97..122) | Get-Random -Count 6 | ForEach-Object { [char]$_ }))"

# Crea Resource Group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Crea ACR (SKU Basic)
az acr create --resource-group $RESOURCE_GROUP --name $ACR_NAME --sku Basic --admin-enabled true

# Ottieni login server
$ACR_LOGIN_SERVER=$(az acr show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query loginServer --output tsv)
$ACR_USERNAME=$(az acr credential show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query username --output tsv)
$ACR_PASSWORD=$(az acr credential show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query passwords[0].value --output tsv)

Write-Output "ACR Login Server: $ACR_LOGIN_SERVER"
```

---

## Passo 2: Build e Push Immagini su ACR

CineBase ha **3 immagini** da buildare e pusare su ACR:

| Immagine | Dockerfile | Contesto |
|---|---|---|
| `cinebase-api` | `backend/Dockerfile` | `./backend` |
| `cinebase-web` | `frontend/CineBase.Web/Dockerfile` | `./frontend/CineBase.Web` |
| `cinebase-seeder` | `backend/scripts/FilmApiSeeder/Dockerfile` | `./backend` |

### Opzione A: Build locale (con Docker Desktop)

```powershell
# Login ad ACR
az acr login --name $ACR_NAME

# Build e push API
docker build -t cinebase-api -f backend/Dockerfile backend
docker tag cinebase-api $ACR_LOGIN_SERVER/cinebase-api:latest
docker push $ACR_LOGIN_SERVER/cinebase-api:latest

# Build e push Frontend
docker build -t cinebase-web -f frontend/CineBase.Web/Dockerfile frontend/CineBase.Web
docker tag cinebase-web $ACR_LOGIN_SERVER/cinebase-web:latest
docker push $ACR_LOGIN_SERVER/cinebase-web:latest

# Build e push Seeder
docker build -t cinebase-seeder -f backend/scripts/FilmApiSeeder/Dockerfile backend
docker tag cinebase-seeder $ACR_LOGIN_SERVER/cinebase-seeder:latest
docker push $ACR_LOGIN_SERVER/cinebase-seeder:latest
```

### Opzione B: Build su ACR (senza Docker locale)

```powershell
az acr build --registry $ACR_NAME --image cinebase-api:latest --file backend/Dockerfile ./backend
az acr build --registry $ACR_NAME --image cinebase-web:latest --file frontend/CineBase.Web/Dockerfile ./frontend/CineBase.Web
az acr build --registry $ACR_NAME --image cinebase-seeder:latest --file backend/scripts/FilmApiSeeder/Dockerfile ./backend
```

---

## Passo 3: Creare ACA Environment

```powershell
# Variabili
$ACA_ENV_NAME="aca-env-cinebase"
$LOG_ANALYTICS_NAME="la-cinebase-prod"

# Crea Log Analytics Workspace
az monitor log-analytics workspace create `
  --resource-group $RESOURCE_GROUP `
  --location $LOCATION `
  --workspace-name $LOG_ANALYTICS_NAME

$LOG_ANALYTICS_CLIENT_ID=$(az monitor log-analytics workspace show --query customerId -g $RESOURCE_GROUP -n $LOG_ANALYTICS_NAME --output tsv)
$LOG_ANALYTICS_CLIENT_SECRET=$(az monitor log-analytics workspace get-shared-keys --query primarySharedKey -g $RESOURCE_GROUP -n $LOG_ANALYTICS_NAME --output tsv)

# Crea ACA Environment
az containerapp env create `
  --name $ACA_ENV_NAME `
  --resource-group $RESOURCE_GROUP `
  --location $LOCATION `
  --logs-workspace-id $LOG_ANALYTICS_CLIENT_ID `
  --logs-workspace-key $LOG_ANALYTICS_CLIENT_SECRET
```

---

## Passo 4: Storage Account e Azure Files per MariaDB

```powershell
# Variabili
$STORAGE_ACCOUNT_NAME="stcinebaseprod$(-join ((97..122) | Get-Random -Count 4 | ForEach-Object { [char]$_ }))"
$FILE_SHARE_NAME_MARIADB="mariadb-data"

# Crea Storage Account
az storage account create `
  --name $STORAGE_ACCOUNT_NAME `
  --resource-group $RESOURCE_GROUP `
  --location $LOCATION `
  --sku Standard_LRS `
  --kind StorageV2

# Ottieni chiave storage
$STORAGE_ACCOUNT_KEY=$(az storage account keys list --resource-group $RESOURCE_GROUP --account-name $STORAGE_ACCOUNT_NAME --query "[0].value" --output tsv)

# Crea condivisione file per MariaDB (5 GB)
az storage share create `
  --name $FILE_SHARE_NAME_MARIADB `
  --account-name $STORAGE_ACCOUNT_NAME `
  --account-key $STORAGE_ACCOUNT_KEY `
  --quota 5

Write-Output "Storage Account: $STORAGE_ACCOUNT_NAME"
Write-Output "File Share: $FILE_SHARE_NAME_MARIADB"
```

### Passo 4.1: Condivisione File per Data Protection Keys

Necessaria per la condivisione delle chiavi di Data Protection ASP.NET Core tra più istanze del frontend (scaling orizzontale).

```powershell
$FILE_SHARE_NAME_DATAPROTECTION="web-dataprotection-keys"

az storage share create `
  --name $FILE_SHARE_NAME_DATAPROTECTION `
  --account-name $STORAGE_ACCOUNT_NAME `
  --account-key $STORAGE_ACCOUNT_KEY `
  --quota 1
```

---

## Passo 5: Segreti in ACA

I segreti ACA sono definiti a livello di singola app container. Segreti condivisi:

| Segreto ACA | Descrizione |
|---|---|
| `mariadb-root-password` | Password root MariaDB |
| `azure-storage-account-key` | Chiave storage account Azure Files |
| `jwt-secret` | Chiave per firma JWT |
| `tmdb-bearer-token` | Token API TMDB |
| `smtp-password` | Password SMTP Gmail |
| `auth-google-clientid` | Client ID Google OAuth |
| `auth-google-clientsecret` | Client Secret Google OAuth |
| `auth-microsoft-clientid` | Client ID Microsoft Entra ID |
| `auth-microsoft-clientsecret` | Client Secret Microsoft Entra ID |

I segreti vengono referenziati nelle env var con `secretref:nome-segreto`.

---

## Passo 6: Deploy MariaDB

```powershell
# Variabili
$MARIADB_APP_NAME="cinebase-db"
$MARIADB_PASSWORD="ScegliUnaPasswordRobustaQui123!"

# Crea Container App per MariaDB
az containerapp create `
  --name $MARIADB_APP_NAME `
  --resource-group $RESOURCE_GROUP `
  --environment $ACA_ENV_NAME `
  --image mariadb:11.4 `
  --min-replicas 0 `
  --max-replicas 1 `
  --secrets `
    mariadb-root-password="$MARIADB_PASSWORD" `
    azure-storage-account-key="$STORAGE_ACCOUNT_KEY" `
  --env-vars `
    MARIADB_ROOT_PASSWORD=secretref:mariadb-root-password `
    MARIADB_DATABASE=film-api-db `
  --azure-file-volume-account-name $STORAGE_ACCOUNT_NAME `
  --azure-file-volume-account-key secretref:azure-storage-account-key `
  --azure-file-volume-share-name $FILE_SHARE_NAME_MARIADB `
  --azure-file-volume-mount-path /var/lib/mysql `
  --target-port 3306 `
  --ingress internal `
  --cpu 0.5 `
  --memory 1Gi
```

> **Nota**: `--min-replicas 0` permette il risparmio sui costi quando il DB non è usato, ma comporta latenza di cold start. Per produzione, considera `--min-replicas 1`.

---

## Passo 7: Deploy Backend API

```powershell
# Variabili
$API_APP_NAME="cinebase-api"
$API_IMAGE="$ACR_LOGIN_SERVER/cinebase-api:latest"
$JWT_SECRET="ScegliUnaChiaveJWTSicuraConAlmeno64Caratteri!"

# Crea Container App per Backend API
az containerapp create `
  --name $API_APP_NAME `
  --resource-group $RESOURCE_GROUP `
  --environment $ACA_ENV_NAME `
  --image $API_IMAGE `
  --registry-server $ACR_LOGIN_SERVER `
  --registry-username $ACR_USERNAME `
  --registry-password $ACR_PASSWORD `
  --min-replicas 0 `
  --max-replicas 3 `
  --secrets `
    mariadb-root-password="$MARIADB_PASSWORD" `
    azure-storage-account-key="$STORAGE_ACCOUNT_KEY" `
    jwt-secret="$JWT_SECRET" `
    tmdb-bearer-token="il_tuo_tmdb_bearer_token_qui" `
  --env-vars `
    ASPNETCORE_ENVIRONMENT=Production `
    ASPNETCORE_URLS=http://+:8080 `
    DB_HOST=cinebase-db `
    DB_PORT=3306 `
    DB_NAME=film-api-db `
    DB_USER=root `
    DB_PASSWORD=secretref:mariadb-root-password `
    DB_USE_AUTODETECT=true `
    DB_SERVER_VERSION=11.4.0-mariadb `
    JWT_SECRET=secretref:jwt-secret `
    JWT_ISSUER=CineBaseAPI `
    JWT_AUDIENCE=CineBaseWeb `
    TMDB_BEARER_TOKEN=secretref:tmdb-bearer-token `
    FRONTEND_BASE_URL=https://<URL_FRONTEND_DOPO_DEPLOY> `
    ALLOWED_CORS_ORIGINS=https://<URL_FRONTEND_DOPO_DEPLOY>,http://localhost:5001 `
    ADMIN_SEED_EMAIL=admin@cinebase.it `
    ADMIN_SEED_PASSWORD=Admin123! `
  --target-port 8080 `
  --ingress internal `
  --cpu 0.5 `
  --memory 1Gi
```

> **Importante**: Sostituisci `<URL_FRONTEND_DOPO_DEPLOY>` con l'URL del frontend dopo il deploy (Passo 9).

### Mappatura env var CineBase → ACA

CineBase legge le variabili d'ambiente direttamente (NON usa il formato `Sezione__Chiave`):

| Env var CineBase | Dove si usa |
|---|---|
| `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` | Connessione MariaDB |
| `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE` | Autenticazione JWT |
| `TMDB_BEARER_TOKEN` | API TMDB per seed film |
| `ALLOWED_CORS_ORIGINS` | CORS (dominio frontend) |
| `FRONTEND_BASE_URL` | URL base frontend per redirect OAuth |
| `ADMIN_SEED_EMAIL`, `ADMIN_SEED_PASSWORD` | Creazione account admin iniziale |
| `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET` | Google OAuth |
| `MICROSOFT_CLIENT_ID`, `MICROSOFT_CLIENT_SECRET`, `MICROSOFT_TENANT_ID` | Microsoft Entra ID OAuth |
| `SMTP_HOST`, `SMTP_PORT`, `SMTP_USER`, `SMTP_PASSWORD` | Invio email (MailKit) |
| `STRIPE_SECRET_API_KEY`, `STRIPE_WEBHOOK_SECRET` | Pagamenti Stripe |

---

## Passo 8: Eseguire il Seeder (ACA Job)

Il seeder è un job one-shot che popola film, cinema, sale e show da TMDB.

```powershell
# Variabili
$SEEDER_JOB_NAME="cinebase-seeder"
$SEEDER_IMAGE="$ACR_LOGIN_SERVER/cinebase-seeder:latest"

# Crea ACA Job
az containerapp job create `
  --name $SEEDER_JOB_NAME `
  --resource-group $RESOURCE_GROUP `
  --environment $ACA_ENV_NAME `
  --image $SEEDER_IMAGE `
  --registry-server $ACR_LOGIN_SERVER `
  --registry-username $ACR_USERNAME `
  --registry-password $ACR_PASSWORD `
  --secrets `
    mariadb-root-password="$MARIADB_PASSWORD" `
    tmdb-bearer-token="il_tuo_tmdb_bearer_token_qui" `
  --env-vars `
    DB_HOST= cinebase-db `
    DB_PORT=3306 `
    DB_NAME=film-api-db `
    DB_USER=root `
    DB_PASSWORD=secretref:mariadb-root-password `
    DB_USE_AUTODETECT=true `
    DB_SERVER_VERSION=11.4.0-mariadb `
    ASPNETCORE_ENVIRONMENT=Production `
    TMDB_BEARER_TOKEN=secretref:tmdb-bearer-token `
  --cpu 0.5 `
  --memory 1Gi

# Esegui il job
az containerapp job start --name $SEEDER_JOB_NAME --resource-group $RESOURCE_GROUP

# Verifica lo stato
az containerapp job execution list --name $SEEDER_JOB_NAME --resource-group $RESOURCE_GROUP --output table
```

> **Nota**: Il seeder può impiegare alcuni minuti per popolare i dati da TMDB.

---

## Passo 9: Deploy Frontend

```powershell
# Variabili
$WEB_APP_NAME="cinebase-web"
$WEB_IMAGE="$ACR_LOGIN_SERVER/cinebase-web:latest"

# Crea Container App per Frontend
az containerapp create `
  --name $WEB_APP_NAME `
  --resource-group $RESOURCE_GROUP `
  --environment $ACA_ENV_NAME `
  --image $WEB_IMAGE `
  --registry-server $ACR_LOGIN_SERVER `
  --registry-username $ACR_USERNAME `
  --registry-password $ACR_PASSWORD `
  --min-replicas 0 `
  --max-replicas 3 `
  --secrets `
    azure-storage-account-key="$STORAGE_ACCOUNT_KEY" `
  --env-vars `
    ASPNETCORE_ENVIRONMENT=Production `
    ASPNETCORE_URLS=http://+:8080 `
    BACKEND_API_URL=http://cinebase-api:8080 `
  --target-port 8080 `
  --ingress external `
  --enable-session-affinity `
  --azure-file-volume-account-name $STORAGE_ACCOUNT_NAME `
  --azure-file-volume-account-key secretref:azure-storage-account-key `
  --azure-file-volume-share-name $FILE_SHARE_NAME_DATAPROTECTION `
  --azure-file-volume-mount-path /mnt/dataprotectionkeys `
  --cpu 0.5 `
  --memory 1Gi

# Ottieni URL pubblico
$WEB_URL=$(az containerapp show --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP --query properties.configuration.ingress.fqdn --output tsv)
Write-Output "Frontend disponibile su: https://$WEB_URL"
```

### Come funziona l'integrazione frontend-backend in ACA

1. Il frontend (`BACKEND_API_URL=http://cinebase-api:8080`) punta al backend via **internal ingress** (rete interna ACA)
2. Il middleware in `Program.cs` injecta `<script>window.API_BASE_URL='http://cinebase-api:8080'</script>` in ogni pagina HTML
3. Il JavaScript usa `window.API_BASE_URL` per chiamare l'API

> **Importante**: Poiché il backend ha internal ingress, il browser chiama il backend tramite il frontend. Se preferisci che il browser chiami direttamente il backend (es. per debugging), dai external ingress anche al backend.

---

## Passo 10: Aggiornare Redirect URI OAuth (Google/Microsoft)

Dopo il deploy, aggiorna i redirect URI sui provider OAuth con l'URL del frontend su ACA:

### Google Cloud Console
- URL: `https://console.cloud.google.com/apis/credentials`
- Redirect URI: `https://<URL_FRONTEND_ACA>/signin-google`

### Microsoft Entra ID
- URL: `https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/`
- Redirect URI: `https://<URL_FRONTEND_ACA>/signin-microsoft`

### Aggiorna env var del backend con gli URL corretti

Dopo aver ottenuto l'URL del frontend, aggiorna il backend:

```powershell
# Aggiorna FRONTEND_BASE_URL e CORS sul backend
az containerapp update --name $API_APP_NAME --resource-group $RESOURCE_GROUP `
  --set `
    environmentVariables[?name=='FRONTEND_BASE_URL'].value=https://$WEB_URL `
    environmentVariables[?name=='ALLOWED_CORS_ORIGINS'].value=https://$WEB_URL,http://localhost:5001
```

---

## Passo 11: Verifica e Troubleshooting

### Controllare i Log

```powershell
# Log in tempo reale backend
az containerapp logs show --name $API_APP_NAME --resource-group $RESOURCE_GROUP --follow

# Log frontend
az containerapp logs show --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP --follow

# Log MariaDB
az containerapp logs show --name $MARIADB_APP_NAME --resource-group $RESOURCE_GROUP --follow

# Log seeder
az containerapp job execution list --name $SEEDER_JOB_NAME --resource-group $RESOURCE_GROUP --output table
az containerapp job logs show --name $SEEDER_JOB_NAME --resource-group $RESOURCE_GROUP --follow
```

### Verifiche rapide

1. **Frontend raggiungibile?** Apri `https://$WEB_URL` nel browser
2. **API risponde?** `curl https://$WEB_URL/api/films` (se backend ha external ingress)
3. **Seed completato?** Login con `admin@cinebase.it` / `Admin123!`
4. **Database persistente?** Riavvia l'app container e verifica i dati

### Comandi utili

```powershell
# Elimina TUTTO (resource group + tutte le risorse)
az group delete --name $RESOURCE_GROUP --yes --no-wait

# Scala a zero per risparmiare
az containerapp update --name $API_APP_NAME --resource-group $RESOURCE_GROUP --min-replicas 0 --max-replicas 0
az containerapp update --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP --min-replicas 0 --max-replicas 0
```

---

## Riepilogo Architettura

| Servizio | Nome ACA | Ingress | Porta | Repliche | Dipende da |
|---|---|---|---|---|---|
| MariaDB | `cinebase-db` | internal | 3306 | 0-1 | - |
| Backend API | `cinebase-api` | internal | 8080 | 0-3 | DB |
| Seeder (Job) | `cinebase-seeder` | - | - | one-shot | DB |
| Frontend | `cinebase-web` | external | 8080 | 0-3 | Backend |

### Costi stimati (italynorth, SKU Basic)

| Servizio | Costo/giorno | Note |
|---|---|---|
| ACR Basic | ~€0.17 | Fisso |
| ACA Environment | ~€0.00 | Incluso nel consumo |
| MariaDB (min 0) | ~€0.00 | Solo quando usato |
| Backend (min 0) | ~€0.00 | Solo quando usato |
| Frontend (min 0) | ~€0.00 | Solo quando usato |
| Storage (5GB LRS) | ~€0.01 | Fisso |
| **Totale** | **~€0.20/giorno** | **~€6/mese** |
