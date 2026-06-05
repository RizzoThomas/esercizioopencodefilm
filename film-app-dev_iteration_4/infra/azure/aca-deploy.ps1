#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploy CineBase su Azure Container Apps (ACA).
.DESCRIPTION
    Crea/aggiorna tutte le risorse Azure necessarie per eseguire CineBase su ACA:
    Resource Group, ACR, ACA Environment, Storage Account, e 4 Container App (db, api, web, seeder-job).

    Prerequisiti:
    - Azure CLI installata e autenticata (az login)
    - Docker Desktop funzionante (per build e push immagini)
    - Sottoscrizione Azure attiva
    - Codice gi+á patchato per ACA (gi+á applicato in questa iterazione):
      * backend/FilmAPI/Program.cs: CORS legge ALLOWED_CORS_ORIGINS da env var
      * frontend/CineBase.Web/Program.cs: middleware injecta BACKEND_API_URL nell'HTML

.PARAMETER ResourceGroup
    Nome del resource group Azure (default: rg-cinebase-prod)
.PARAMETER Location
    Regione Azure (default: italynorth)
.PARAMETER AcrName
    Nome Azure Container Registry (default: acrcinebase<random>)
.PARAMETER AcaEnvName
    Nome ACA Environment (default: aca-env-cinebase)
.PARAMETER StorageSku
    SKU storage account (default: Standard_LRS)
.PARAMETER DbPassword
    Password root MariaDB (se non fornita, viene generata)
.PARAMETER TmdbToken
    TMDB Bearer Token per il seeder (OBBLIGATORIO)
.PARAMETER JwtSecret
    JWT Secret (default: autogenerato)
.PARAMETER AdminEmail
    Email admin seed (default: admin@cinebase.it)
.PARAMETER AdminPassword
    Password admin seed (default: autogenerata)
.PARAMETER FrontendExternalUrl
    URL pubblico del frontend (es. https://cinebase-web.xyz.italynorth.azurecontainerapps.io)
    Se omesso, viene letto dopo il deploy.
#>

param(
    [string]$ResourceGroup = "rg-cinebase-prod",
    [string]$Location = "westeurope",
    [string]$AcrName,
    [string]$AcaEnvName = "aca-env-cinebase",
    [string]$StorageSku = "Standard_LRS",
    [string]$DbPassword,
    [string]$TmdbToken = "",
    [string]$JwtSecret,
    [string]$AdminEmail = "admin@cinebase.it",
    [string]$AdminPassword,
    [string]$FrontendExternalUrl = ""
)

# ------ FUNCTIONS ------
function Write-Step { param([string]$Msg) Write-Host "`n=== $Msg ===" -ForegroundColor Cyan }
function Write-Info { param([string]$Msg) Write-Host "  $Msg" -ForegroundColor Gray }
function Write-OK { param([string]$Msg) Write-Host "  [OK] $Msg" -ForegroundColor Green }
function Write-Warn { param([string]$Msg) Write-Host "  [WARN] $Msg" -ForegroundColor Yellow }
function Write-Err { param([string]$Msg) Write-Host "  [ERR] $Msg" -ForegroundColor Red; exit 1 }

# ------ VALIDAZIONE PRELIMINARE ------
Write-Step "Preflight checks"

# Verifica Azure CLI
if (-not (Get-Command "az" -ErrorAction SilentlyContinue)) {
    Write-Err "Azure CLI non trovata. Installala da https://aka.ms/installazurecliwindows"
}

# Verifica login Azure
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Warn "Esegui 'az login' per autenticarti."
    az login
    $account = az account show 2>$null | ConvertFrom-Json
    if (-not $account) { Write-Err "Login fallito." }
}
Write-OK "Azure account: $($account.name) ($($account.user.name))"

# Verifica Docker
$dockerOK = docker ps 2>$null
if (-not $dockerOK) {
    Write-Warn "Docker non disponibile. Le immagini verranno buildate su ACR via 'az acr build'."
    $UseAcrBuild = $true
} else {
    Write-OK "Docker disponibile"
    $UseAcrBuild = $false
}

# Verifica TMDB token
if ([string]::IsNullOrWhiteSpace($TmdbToken)) {
    Write-Err "Parametro -TmdbToken obbligatorio. Ottieni un token da https://www.themoviedb.org/settings/api"
}

# Genera valori di default per i segreti
if (-not $DbPassword) { $DbPassword = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 24 | ForEach-Object { [char]$_ }) }
if (-not $JwtSecret) { $JwtSecret = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object { [char]$_ }) }
if (-not $AdminPassword) { $AdminPassword = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 16 | ForEach-Object { [char]$_ }) + "!" }

Write-Info "Admin email: $AdminEmail"
Write-Info "DB Password: [nascosta]"
Write-Info "JWT Secret: [nascosto]"

# Nomi risorse (con random suffix per unicità ACR)
$suffix = -join ((97..122) | Get-Random -Count 6 | ForEach-Object { [char]$_ })
if (-not $AcrName) { $AcrName = "acrcinebase${suffix}" }

# Nomi app container
$DbAppName = "cinebase-db"
$ApiAppName = "cinebase-api"
$WebAppName = "cinebase-web"
$SeederJobName = "cinebase-seeder"

# Path repo root (assume script in infra/azure/)
$RepoRoot = Resolve-Path "$PSScriptRoot/../.."

# ------ 1. RESOURCE GROUP ------
Write-Step "1. Resource Group"
az group create --name $ResourceGroup --location $Location --output none
Write-OK "Resource Group: $ResourceGroup"

# ------ 2. AZURE CONTAINER REGISTRY ------
Write-Step "2. Azure Container Registry (ACR)"
$acrCheck = az acr show --name $AcrName --resource-group $ResourceGroup 2>$null
if (-not $acrCheck) {
    az acr create --resource-group $ResourceGroup --name $AcrName --sku Basic --admin-enabled true --output none
    Write-OK "ACR creato: $AcrName"
} else {
    Write-OK "ACR esistente: $AcrName"
}
$AcrLoginServer = az acr show --name $AcrName --resource-group $ResourceGroup --query loginServer --output tsv
$AcrUsername = az acr credential show --name $AcrName --resource-group $ResourceGroup --query username --output tsv
$AcrPassword = az acr credential show --name $AcrName --resource-group $ResourceGroup --query passwords[0].value --output tsv
Write-Info "Login server: $AcrLoginServer"

# ------ 3. BUILD & PUSH IMMAGINI ------
Write-Step "3. Build & Push immagini su ACR"

function Push-Image {
    param([string]$ContextPath, [string]$Dockerfile, [string]$ImageName)

    $tag = "$AcrLoginServer/${ImageName}:latest"
    Write-Info "Building ${ImageName}..."

    if ($UseAcrBuild) {
        # Build direttamente su ACR (non serve Docker locale)
        # NOTA: ACR Tasks potrebbe non essere disponibile su Azure for Students
        # In tal caso, usa GitHub Actions: .github/workflows/deploy-aca.yml
        $imageTag = "${ImageName}:latest"
        az acr build -r $AcrName -t $imageTag -f $Dockerfile $ContextPath --no-logs 2>$null
        if (-not $?) {
            Write-Warn "ACR Tasks non disponibile su questa subscription. Usa GitHub Actions workflow."
            Write-Warn "  https://github.com/GreppiDev/Info5IA2526WebDev/actions"
        }
    } else {
        # Build locale + push
        docker build -t $ImageName -f $Dockerfile $ContextPath
        docker tag $ImageName $tag
        az acr login --name $AcrName
        docker push $tag
    }
    Write-OK "Immagine pushatA: $tag"
}

Push-Image -ContextPath "$RepoRoot/backend" -Dockerfile "$RepoRoot/backend/Dockerfile" -ImageName "cinebase-api"
Push-Image -ContextPath "$RepoRoot/frontend/CineBase.Web" -Dockerfile "$RepoRoot/frontend/CineBase.Web/Dockerfile" -ImageName "cinebase-web"
Push-Image -ContextPath "$RepoRoot/backend" -Dockerfile "$RepoRoot/backend/scripts/FilmApiSeeder/Dockerfile" -ImageName "cinebase-seeder"

# ------ 4. ACA ENVIRONMENT + LOG ANALYTICS ------
Write-Step "4. ACA Environment"

$LogAnalyticsName = "la-cinebase-${suffix}"

$laCheck = az monitor log-analytics workspace show --resource-group $ResourceGroup --workspace-name $LogAnalyticsName 2>$null
if (-not $laCheck) {
    az monitor log-analytics workspace create --resource-group $ResourceGroup --location $Location `
        --workspace-name $LogAnalyticsName --output none
    Write-OK "Log Analytics creato: $LogAnalyticsName"
} else {
    Write-OK "Log Analytics esistente: $LogAnalyticsName"
}

$LogAnalyticsClientId = az monitor log-analytics workspace show --query customerId `
    -g $ResourceGroup -n $LogAnalyticsName --output tsv
$LogAnalyticsKey = az monitor log-analytics workspace get-shared-keys --query primarySharedKey `
    -g $ResourceGroup -n $LogAnalyticsName --output tsv

$envCheck = az containerapp env show --name $AcaEnvName --resource-group $ResourceGroup 2>$null
if (-not $envCheck) {
    az containerapp env create --name $AcaEnvName --resource-group $ResourceGroup --location $Location `
        --logs-workspace-id $LogAnalyticsClientId --logs-workspace-key $LogAnalyticsKey --output none
    Write-OK "ACA Environment creato: $AcaEnvName"
} else {
    Write-OK "ACA Environment esistente: $AcaEnvName"
}

# ------ 5. STORAGE ACCOUNT + AZURE FILES ------
Write-Step "5. Storage Account e Azure Files"

$StorageAccountName = "stcinebase${suffix}"
$saCheck = az storage account show --name $StorageAccountName --resource-group $ResourceGroup 2>$null
if (-not $saCheck) {
    az storage account create --name $StorageAccountName --resource-group $ResourceGroup `
        --location $Location --sku $StorageSku --kind StorageV2 --output none
    Write-OK "Storage Account creato: $StorageAccountName"
} else {
    Write-OK "Storage Account esistente: $StorageAccountName"
}

$StorageAccountKey = az storage account keys list --resource-group $ResourceGroup `
    --account-name $StorageAccountName --query "[0].value" --output tsv

# Condivisione MariaDB
$MariaDbShareName = "mariadb-data"
az storage share create --name $MariaDbShareName --account-name $StorageAccountName `
    --account-key $StorageAccountKey --quota 5 --output none 2>$null
Write-OK "File share '$MariaDbShareName' pronto (5GB)"

# Condivisione Data Protection (per scaling orizzontale futuro)
$DataProtectionShareName = "web-dataprotection-keys"
az storage share create --name $DataProtectionShareName --account-name $StorageAccountName `
    --account-key $StorageAccountKey --quota 1 --output none 2>$null
Write-OK "File share '$DataProtectionShareName' pronto (1GB)"

# ------ 6. DEPLOY MARIADB ------
Write-Step "6. Deploy MariaDB"

# Prepara segreti condivisi
$Secrets = @(
    "mariadb-root-password=$DbPassword",
    "azure-storage-account-key=$StorageAccountKey"
) -join " "

Write-Info "Creazione Container App: $DbAppName (MariaDB)..."
az containerapp create `
    --name $DbAppName `
    --resource-group $ResourceGroup `
    --environment $AcaEnvName `
    --image mariadb:11.4 `
    --min-replicas 0 `
    --max-replicas 1 `
    --secrets $Secrets `
    --env-vars "MARIADB_ROOT_PASSWORD=secretref:mariadb-root-password" `
               "MARIADB_DATABASE=film-api-db" `
    --target-port 3306 `
    --ingress internal `
    --cpu 0.5 `
    --memory 1Gi `
    --output none

# Monta volume Azure Files per persistenza dati (sintassi corrente ACA extension)
Write-Info "Montaggio volume Azure Files per MariaDB..."
$dbVolumeJs = "{""name"":""mariadb-data"",""storageName"":""$MariaDbShareName"",""storageType"":""AzureFile""}"
$mountJs = "{""volumeName"":""mariadb-data"",""mountPath"":""/var/lib/mysql""}"
az containerapp update --name $DbAppName --resource-group $ResourceGroup `
    --set "template.volumes=[$dbVolumeJs]" `
    --set "template.containers[0].volumeMounts=[$mountJs]" `
    --output none 2>&1 | Select-String -NotMatch "WARNING"
Write-OK "MariaDB deployato (ingress interno + volume persistente)"

# ------ 7. DEPLOY BACKEND API ------
Write-Step "7. Deploy Backend API"

$ApiImage = "$AcrLoginServer/cinebase-api:latest"
$ConnectionString = "Server=${DbAppName};Port=3306;Database=film-api-db;User Id=root;Pwd=secretref:mariadb-root-password;"

# Segreti aggiuntivi per API
$ApiSecrets = @(
    "mariadb-root-password=$DbPassword",
    "azure-storage-account-key=$StorageAccountKey",
    "jwt-secret=$JwtSecret",
    "tmdb-bearer-token=$TmdbToken"
) -join " "

# Env vars per API
$WebFqdnTemp = az containerapp show --name $WebAppName --resource-group $ResourceGroup `
    --query properties.configuration.ingress.fqdn --output tsv 2>$null
$FrontendUrl = if ($WebFqdnTemp) { "https://${WebFqdnTemp}" } else { "http://localhost:5001" }

$ApiEnvVars = @(
    "ASPNETCORE_ENVIRONMENT=Production",
    "ASPNETCORE_URLS=http://+:8080",
    "DB_HOST=${DbAppName}",
    "DB_PORT=3306",
    "DB_NAME=film-api-db",
    "DB_USER=root",
    "DB_PASSWORD=secretref:mariadb-root-password",
    "DB_USE_AUTODETECT=true",
    "DB_SERVER_VERSION=11.4.0-mariadb",
    "JWT_SECRET=secretref:jwt-secret",
    "JWT_ISSUER=CineBaseAPI",
    "JWT_AUDIENCE=CineBaseWeb",
    "TMDB_BEARER_TOKEN=secretref:tmdb-bearer-token",
    "ALLOWED_CORS_ORIGINS=${FrontendUrl},http://localhost:5001"
) -join " "

# Aggiungi ADMIN_SEED se fornito
if ($AdminEmail -and $AdminPassword) {
    $ApiEnvVars += " ADMIN_SEED_EMAIL=$AdminEmail ADMIN_SEED_PASSWORD=$AdminPassword"
}

Write-Info "Creazione Container App: $ApiAppName..."
az containerapp create `
    --name $ApiAppName `
    --resource-group $ResourceGroup `
    --environment $AcaEnvName `
    --image $ApiImage `
    --registry-server $AcrLoginServer `
    --registry-username $AcrUsername `
    --registry-password $AcrPassword `
    --min-replicas 0 `
    --max-replicas 3 `
    --secrets $ApiSecrets `
    --env-vars $ApiEnvVars `
    --target-port 8080 `
    --ingress internal `
    --cpu 0.5 `
    --memory 1Gi `
    --output none

# Ottieni URL interno API
$ApiInternalUrl = "http://${ApiAppName}:8080"
Write-OK "Backend API deployato (ingress interno): $ApiInternalUrl"

# ------ 8. DEPLOY SEEDER (ACA JOB) ------
Write-Step "8. Deploy Seeder (ACA Job)"

$SeederImage = "$AcrLoginServer/cinebase-seeder:latest"

$SeederEnvVars = @(
    "ASPNETCORE_ENVIRONMENT=Production",
    "DB_HOST=${DbAppName}",
    "DB_PORT=3306",
    "DB_NAME=film-api-db",
    "DB_USER=root",
    "DB_PASSWORD=secretref:mariadb-root-password",
    "DB_USE_AUTODETECT=true",
    "DB_SERVER_VERSION=11.4.0-mariadb",
    "TMDB_BEARER_TOKEN=secretref:tmdb-bearer-token"
) -join " "

if ($AdminEmail -and $AdminPassword) {
    $SeederEnvVars += " ADMIN_SEED_EMAIL=$AdminEmail ADMIN_SEED_PASSWORD=$AdminPassword"
}

Write-Info "Creazione ACA Job: $SeederJobName..."
az containerapp job create `
    --name $SeederJobName `
    --resource-group $ResourceGroup `
    --environment $AcaEnvName `
    --image $SeederImage `
    --registry-server $AcrLoginServer `
    --registry-username $AcrUsername `
    --registry-password $AcrPassword `
    --trigger-type Manual `
    --secrets "mariadb-root-password=$DbPassword" "tmdb-bearer-token=$TmdbToken" `
    --env-vars $SeederEnvVars `
    --cpu 0.5 `
    --memory 1Gi `
    --output none

Write-OK "ACA Job '$SeederJobName' creato"

# Esegui il seeder
Write-Info "Esecuzione seeder (potrebbero volerci alcuni minuti)..."
az containerapp job start --name $SeederJobName --resource-group $ResourceGroup --output none
Write-OK "Seeder avviato. Verifica con: az containerapp job execution list -g $ResourceGroup -n $SeederJobName"

# ------ 9. DEPLOY FRONTEND ------
Write-Step "9. Deploy Frontend"

$WebImage = "$AcrLoginServer/cinebase-web:latest"

# Determina URL frontend
if ([string]::IsNullOrWhiteSpace($FrontendExternalUrl)) {
    $WebFqdn = az containerapp show --name $WebAppName --resource-group $ResourceGroup `
        --query properties.configuration.ingress.fqdn --output tsv 2>$null
    if ($WebFqdn) {
        $FrontendExternalUrl = "https://${WebFqdn}"
    } else {
        $FrontendExternalUrl = "https://PLACEHOLDER" # Verrà aggiornato dopo il deploy
    }
}

# L'URL interno del backend (usato dal frontend per le chiamate API via middleware)
$BackendInternalUrl = "http://${ApiAppName}:8080"

# Env vars per frontend
$WebEnvVars = @(
    "ASPNETCORE_ENVIRONMENT=Production",
    "ASPNETCORE_URLS=http://+:8080",
    "BACKEND_API_URL=${BackendInternalUrl}"
) -join " "

Write-Info "Creazione Container App: $WebAppName (Frontend)..."
az containerapp create `
    --name $WebAppName `
    --resource-group $ResourceGroup `
    --environment $AcaEnvName `
    --image $WebImage `
    --registry-server $AcrLoginServer `
    --registry-username $AcrUsername `
    --registry-password $AcrPassword `
    --min-replicas 0 `
    --max-replicas 3 `
    --secrets $Secrets `
    --env-vars $WebEnvVars `
    --target-port 8080 `
    --ingress external `
    --cpu 0.5 `
    --memory 1Gi `
    --output none

# Ottieni URL pubblico
$WebFqdn = az containerapp show --name $WebAppName --resource-group $ResourceGroup `
    --query properties.configuration.ingress.fqdn --output tsv
$WebUrl = "https://${WebFqdn}"
Write-OK "Frontend deployato: $WebUrl"

# ------ 10. POST-DEPLOY CONFIG ------
Write-Step "10. Post-deploy: Verifica URL e Redirect"

Write-Info "Per OAuth con Google/Microsoft, aggiorna i redirect URI con:"
Write-Info "  $WebUrl/signin-google"
Write-Info "  $WebUrl/signin-microsoft"

# Mostra comandi utili
Write-Step "Riepilogo deploy"
Write-Info "Resource Group:    $ResourceGroup"
Write-Info "ACR:               $AcrLoginServer"
Write-Info "ACA Environment:   $AcaEnvName"
Write-Info "MariaDB (int):     $DbAppName"
Write-Info "Backend API (int): $ApiAppName"
Write-Info "Frontend (ext):    $WebUrl"
Write-Info "Seeder Job:        $SeederJobName"
Write-Info "Storage Account:   $StorageAccountName"
Write-Info ""
Write-Info "Admin email:       $AdminEmail"
Write-Info "Admin password:    $AdminPassword (salvala!)"
Write-Info ""
Write-Info "Comandi utili:"
Write-Info "  Log stream backend:  az containerapp logs show -n $ApiAppName -g $ResourceGroup --follow"
Write-Info "  Log stream frontend: az containerapp logs show -n $WebAppName -g $ResourceGroup --follow"
Write-Info "  Esegui seeder:       az containerapp job start -n $SeederJobName -g $ResourceGroup"
Write-Info "  Mostra seeder logs:  az containerapp job execution list -n $SeederJobName -g $ResourceGroup -o table"
Write-Info "  Elimina tutto:       az group delete -n $ResourceGroup --yes --no-wait"
