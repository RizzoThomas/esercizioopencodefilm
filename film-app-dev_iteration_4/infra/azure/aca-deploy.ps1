#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploy CineBase su Azure Container Apps (ACA).

.DESCRIPTION
    Crea/aggiorna tutte le risorse Azure necessarie per eseguire CineBase su ACA:
    Resource Group, ACR, ACA Environment, Storage Account, e 4 Container App (db, api, web, seeder-job).

    Prerequisiti:
    - Azure CLI installata e autenticata (az login)
    - Docker Desktop funzionante (per build e push immagini, oppure usa az acr build)
    - Sottoscrizione Azure attiva
    - TMDB Bearer Token (da https://www.themoviedb.org/settings/api)

.PARAMETER ResourceGroup
    Nome del resource group Azure (default: rg-cinebase-prod)
.PARAMETER Location
    Regione Azure (default: italynorth)
.PARAMETER AcrName
    Nome Azure Container Registry (default: acrcinebase<random6>)
.PARAMETER AcaEnvName
    Nome ACA Environment (default: aca-env-cinebase)
.PARAMETER StorageSku
    SKU storage account (default: Standard_LRS)
.PARAMETER DbPassword
    Password root MariaDB (se non fornita, viene generata)
.PARAMETER TmdbToken
    TMDB Bearer Token per il seeder (OBBLIGATORIO)
.PARAMETER JwtSecret
    JWT Secret (default: autogenerato 64 char)
.PARAMETER AdminEmail
    Email admin seed (default: admin@cinebase.it)
.PARAMETER AdminPassword
    Password admin seed (default: autogenerata)
.PARAMETER FrontendExternalUrl
    URL pubblico del frontend (es. https://cinebase-web.xyz.italynorth.azurecontainerapps.io)
    Se omesso, viene letto dopo il deploy.
.PARAMETER WhatIf
    Mostra le azioni che verranno eseguite senza eseguirle realmente.

.EXAMPLE
    .\infra\azure\aca-deploy.ps1 -TmdbToken "eyJhbGciOiJIUzI1NiJ9..."

.EXAMPLE
    .\infra\azure\aca-deploy.ps1 -TmdbToken "..." -ResourceGroup "rg-cinebase-test" -Location "westeurope" -WhatIf
#>

param(
    [string]$ResourceGroup = "rg-cinebase-prod",
    [string]$Location = "italynorth",
    [string]$AcrName,
    [string]$AcaEnvName = "aca-env-cinebase",
    [string]$StorageSku = "Standard_LRS",
    [string]$DbPassword,
    [string]$TmdbToken = "",
    [string]$JwtSecret,
    [string]$AdminEmail = "admin@cinebase.it",
    [string]$AdminPassword,
    [string]$FrontendExternalUrl = "",
    [switch]$WhatIf = $false
)

# ------ FUNCTIONS ------
function Write-Step { param([string]$Msg) Write-Host "`n=== $Msg ===" -ForegroundColor Cyan }
function Write-Info { param([string]$Msg) Write-Host "  $Msg" -ForegroundColor Gray }
function Write-OK { param([string]$Msg) Write-Host "  [OK] $Msg" -ForegroundColor Green }
function Write-Warn { param([string]$Msg) Write-Host "  [WARN] $Msg" -ForegroundColor Yellow }
function Write-Err { param([string]$Msg) Write-Host "  [ERR] $Msg" -ForegroundColor Red; exit 1 }

function Invoke-AzCommand {
    param(
        [string]$Command,
        [string]$Description,
        [int]$Retries = 1,
        [int]$RetryDelaySeconds = 10
    )

    if ($WhatIf) {
        Write-Info "[WHATIF] $Description"
        Write-Info "         az $Command"
        return $null
    }

    $attempt = 0
    do {
        $attempt++
        try {
            $result = Invoke-Expression "az $Command 2>&1"
            $exitCode = $LASTEXITCODE
            if ($exitCode -eq 0) {
                return $result
            }
            throw "Exit code: $exitCode`n$result"
        }
        catch {
            if ($attempt -lt $Retries) {
                Write-Warn "Tentativo $attempt/$Retries fallito: $($_.Exception.Message)"
                Write-Info "Nuovo tentativo tra ${RetryDelaySeconds}s..."
                Start-Sleep -Seconds $RetryDelaySeconds
            }
            else {
                Write-Err "$Description fallito dopo $attempt tentativi: $($_.Exception.Message)"
            }
        }
    } while ($attempt -lt $Retries)
}

# ------ VALIDAZIONE PRELIMINARE ------
Write-Step "Preflight checks"

# Verifica Azure CLI
if (-not (Get-Command "az" -ErrorAction SilentlyContinue)) {
    Write-Err "Azure CLI non trovata. Installala da https://aka.ms/installazurecliwindows"
}

# Verifica login Azure
try {
    $account = az account show 2>$null | ConvertFrom-Json
    if (-not $account) { throw "Non autenticato" }
    Write-OK "Azure account: $($account.name) ($($account.user.name))"
}
catch {
    Write-Warn "Esegui 'az login' per autenticarti."
    az login
    $account = az account show 2>$null | ConvertFrom-Json
    if (-not $account) { Write-Err "Login fallito." }
    Write-OK "Azure account: $($account.name) ($($account.user.name))"
}

# Verifica Docker
$dockerOK = docker ps 2>$null
if (-not $dockerOK) {
    Write-Warn "Docker non disponibile. Le immagini verranno buildate su ACR via 'az acr build'."
    $global:UseAcrBuild = $true
}
else {
    Write-OK "Docker disponibile"
    $global:UseAcrBuild = $false
}

# Verifica TMDB token
if ([string]::IsNullOrWhiteSpace($TmdbToken)) {
    Write-Err "Parametro -TmdbToken obbligatorio. Ottieni un token da https://www.themoviedb.org/settings/api"
}

# Genera valori di default per i segreti
$charPool = (65..90) + (97..122) + (48..57)
if (-not $DbPassword) { $DbPassword = -join ($charPool | Get-Random -Count 24 | ForEach-Object { [char]$_ }) + "!" }
if (-not $JwtSecret) { $JwtSecret = -join ($charPool | Get-Random -Count 64 | ForEach-Object { [char]$_ }) }
if (-not $AdminPassword) { $AdminPassword = -join ($charPool | Get-Random -Count 16 | ForEach-Object { [char]$_ }) + "!" }

Write-Info "Admin email:       $AdminEmail"
Write-Info "Admin password:    [nascosta]"
Write-Info "DB Password:       [nascosta]"
Write-Info "JWT Secret:        [nascosto]"
Write-Info "TMDB Token:        [nascosto]"

# Nomi risorse
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
Invoke-AzCommand -Command "group create --name $ResourceGroup --location $Location --output none" `
    -Description "Creazione Resource Group: $ResourceGroup"
Write-OK "Resource Group: $ResourceGroup"

# ------ 2. AZURE CONTAINER REGISTRY ------
Write-Step "2. Azure Container Registry (ACR)"
Invoke-AzCommand -Command "acr create --resource-group $ResourceGroup --name $AcrName --sku Basic --admin-enabled true --output none" `
    -Description "Creazione ACR: $AcrName" `
    -Retries 1
$AcrLoginServer = az acr show --name $AcrName --resource-group $ResourceGroup --query loginServer --output tsv
$AcrUsername = az acr credential show --name $AcrName --resource-group $ResourceGroup --query username --output tsv
$AcrPassword = az acr credential show --name $AcrName --resource-group $ResourceGroup --query passwords[0].value --output tsv
Write-OK "ACR: $AcrLoginServer"

# ------ 3. BUILD & PUSH IMMAGINI ------
Write-Step "3. Build & Push immagini su ACR"

function Push-Image {
    param([string]$ContextPath, [string]$Dockerfile, [string]$ImageName)

    $tag = "${AcrLoginServer}/${ImageName}:latest"
    Write-Info "Building ${ImageName}..."

    if ($WhatIf) {
        Write-Info "[WHATIF] Build e push immagine ${ImageName} su ACR"
        return
    }

    if ($global:UseAcrBuild) {
        Write-Info "Build su ACR (az acr build)..."
        Invoke-AzCommand -Command "acr build -r $AcrName -t ${ImageName}:latest -f $Dockerfile $ContextPath --no-logs" `
            -Description "Build ACR: ${ImageName}" `
            -Retries 2 -RetryDelaySeconds 15
    }
    else {
        Write-Info "Build locale Docker..."
        $buildResult = docker build -t $ImageName -f $Dockerfile $ContextPath 2>&1
        if ($LASTEXITCODE -ne 0) { Write-Err "Build fallita per ${ImageName}: $buildResult" }

        docker tag $ImageName $tag
        az acr login --name $AcrName
        docker push $tag
        if ($LASTEXITCODE -ne 0) { Write-Err "Push fallita per ${ImageName}" }
    }
    Write-OK "Immagine pronta: $tag"
}

Push-Image -ContextPath "$RepoRoot/backend" -Dockerfile "$RepoRoot/backend/Dockerfile" -ImageName "cinebase-api"
Push-Image -ContextPath "$RepoRoot/frontend/CineBase.Web" -Dockerfile "$RepoRoot/frontend/CineBase.Web/Dockerfile" -ImageName "cinebase-web"
Push-Image -ContextPath "$RepoRoot/backend" -Dockerfile "$RepoRoot/backend/scripts/FilmApiSeeder/Dockerfile" -ImageName "cinebase-seeder"

# ------ 4. ACA ENVIRONMENT + LOG ANALYTICS ------
Write-Step "4. ACA Environment"

$LogAnalyticsName = "la-cinebase-${suffix}"

Invoke-AzCommand -Command "monitor log-analytics workspace create --resource-group $ResourceGroup --location $Location --workspace-name $LogAnalyticsName --output none" `
    -Description "Creazione Log Analytics: $LogAnalyticsName"

$LogAnalyticsClientId = az monitor log-analytics workspace show --query customerId -g $ResourceGroup -n $LogAnalyticsName --output tsv
$LogAnalyticsKey = az monitor log-analytics workspace get-shared-keys --query primarySharedKey -g $ResourceGroup -n $LogAnalyticsName --output tsv

Invoke-AzCommand -Command "containerapp env create --name $AcaEnvName --resource-group $ResourceGroup --location $Location --logs-workspace-id $LogAnalyticsClientId --logs-workspace-key $LogAnalyticsKey --output none" `
    -Description "Creazione ACA Environment: $AcaEnvName"

# ------ 5. STORAGE ACCOUNT + AZURE FILES ------
Write-Step "5. Storage Account e Azure Files"

$StorageAccountName = "stcinebase${suffix}"
Invoke-AzCommand -Command "storage account create --name $StorageAccountName --resource-group $ResourceGroup --location $Location --sku $StorageSku --kind StorageV2 --output none" `
    -Description "Creazione Storage Account: $StorageAccountName"

$StorageAccountKey = az storage account keys list --resource-group $ResourceGroup --account-name $StorageAccountName --query "[0].value" --output tsv
if (-not $StorageAccountKey) { Write-Err "Impossibile ottenere la chiave dello storage account" }

# Condivisione MariaDB (5 GB)
$MariaDbShareName = "mariadb-data"
Invoke-AzCommand -Command "storage share create --name $MariaDbShareName --account-name $StorageAccountName --account-key $StorageAccountKey --quota 5 --output none" `
    -Description "Creazione File Share MariaDB (5GB)"
Write-OK "File share '$MariaDbShareName' pronto (5GB)"

# Condivisione Data Protection (1 GB) per scaling orizzontale frontend
$DataProtectionShareName = "web-dataprotection-keys"
Invoke-AzCommand -Command "storage share create --name $DataProtectionShareName --account-name $StorageAccountName --account-key $StorageAccountKey --quota 1 --output none" `
    -Description "Creazione File Share Data Protection (1GB)"
Write-OK "File share '$DataProtectionShareName' pronto (1GB)"

# Segreti condivisi
$SharedSecrets = "mariadb-root-password=$DbPassword azure-storage-account-key=$StorageAccountKey"

# ------ 6. DEPLOY MARIADB ------
Write-Step "6. Deploy MariaDB"

Write-Info "Creazione Container App: $DbAppName (MariaDB)..."
Invoke-AzCommand -Command "containerapp create --name $DbAppName --resource-group $ResourceGroup --environment $AcaEnvName --image mariadb:11.4 --min-replicas 0 --max-replicas 1 --secrets $SharedSecrets --env-vars MARIADB_ROOT_PASSWORD=secretref:mariadb-root-password MARIADB_DATABASE=film-api-db --target-port 3306 --ingress internal --cpu 0.5 --memory 1Gi --output none" `
    -Description "Creazione Container App: $DbAppName (MariaDB)"

# Monta volume Azure Files per MariaDB
if (-not $WhatIf) {
    Write-Info "Montaggio volume Azure Files per MariaDB..."
    $dbVolumeJs = "{`"name`":`"mariadb-data`",`"storageName`":`"$MariaDbShareName`",`"storageType`":`"AzureFile`"}"
    $mountJs = "{`"volumeName`":`"mariadb-data`",`"mountPath`":`"/var/lib/mysql`"}"
    $volumeResult = az containerapp update --name $DbAppName --resource-group $ResourceGroup `
        --set "template.volumes=[$dbVolumeJs]" `
        --set "template.containers[0].volumeMounts=[$mountJs]" `
        --output none 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Warn "Montaggio volume MariaDB: $volumeResult" }
}
Write-OK "MariaDB deployato (ingress interno + volume persistente)"

# ------ 7. DEPLOY BACKEND API ------
Write-Step "7. Deploy Backend API"

$ApiImage = "${AcrLoginServer}/cinebase-api:latest"

# Prepara URL frontend per CORS e redirect
$WebFqdnTemp = az containerapp show --name $WebAppName --resource-group $ResourceGroup --query properties.configuration.ingress.fqdn --output tsv 2>$null
$FrontendUrl = if ($WebFqdnTemp) { "https://${WebFqdnTemp}" } else { "http://localhost:5001" }

$ApiSecrets = "mariadb-root-password=$DbPassword azure-storage-account-key=$StorageAccountKey jwt-secret=$JwtSecret tmdb-bearer-token=$TmdbToken"
$ApiEnvVars = "ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://+:8080 DB_HOST=${DbAppName} DB_PORT=3306 DB_NAME=film-api-db DB_USER=root DB_PASSWORD=secretref:mariadb-root-password DB_USE_AUTODETECT=true DB_SERVER_VERSION=11.4.0-mariadb JWT_SECRET=secretref:jwt-secret JWT_ISSUER=CineBaseAPI JWT_AUDIENCE=CineBaseWeb TMDB_BEARER_TOKEN=secretref:tmdb-bearer-token ALLOWED_CORS_ORIGINS=${FrontendUrl},http://localhost:5001 FRONTEND_BASE_URL=${FrontendUrl} ADMIN_SEED_EMAIL=${AdminEmail} ADMIN_SEED_PASSWORD=${AdminPassword}"

Write-Info "Creazione Container App: $ApiAppName..."
Invoke-AzCommand -Command "containerapp create --name $ApiAppName --resource-group $ResourceGroup --environment $AcaEnvName --image $ApiImage --registry-server $AcrLoginServer --registry-username $AcrUsername --registry-password $AcrPassword --min-replicas 0 --max-replicas 3 --secrets $ApiSecrets --env-vars $ApiEnvVars --target-port 8080 --ingress internal --cpu 0.5 --memory 1Gi --output none" `
    -Description "Creazione Container App: $ApiAppName (Backend)"

# Configura health probe per backend
if (-not $WhatIf) {
    Write-Info "Configurazione health probe per il backend..."
    $probeResult = az containerapp update --name $ApiAppName --resource-group $ResourceGroup `
        --set "template.containers[0].livenessProbe={`"path`":`"/health`",`"port`":8080,`"type`":`"http`",`"initialDelaySeconds`":30,`"periodSeconds`":30}" `
        --set "template.containers[0].readinessProbe={`"path`":`"/health`",`"port`":8080,`"type`":`"http`",`"initialDelaySeconds`":15,`"periodSeconds`":15}" `
        --output none 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Warn "Configurazione health probe: $probeResult" }
}

$ApiInternalUrl = "http://${ApiAppName}:8080"
Write-OK "Backend API deployato (ingress interno): $ApiInternalUrl"

# ------ 8. DEPLOY SEEDER (ACA JOB) ------
Write-Step "8. Deploy Seeder (ACA Job)"

$SeederImage = "${AcrLoginServer}/cinebase-seeder:latest"
$SeederSecrets = "mariadb-root-password=$DbPassword tmdb-bearer-token=$TmdbToken"
$SeederEnvVars = "ASPNETCORE_ENVIRONMENT=Production DB_HOST=${DbAppName} DB_PORT=3306 DB_NAME=film-api-db DB_USER=root DB_PASSWORD=secretref:mariadb-root-password DB_USE_AUTODETECT=true DB_SERVER_VERSION=11.4.0-mariadb TMDB_BEARER_TOKEN=secretref:tmdb-bearer-token ADMIN_SEED_EMAIL=${AdminEmail} ADMIN_SEED_PASSWORD=${AdminPassword} FRONTEND_BASE_URL=${FrontendUrl}"

Write-Info "Creazione ACA Job: $SeederJobName..."
Invoke-AzCommand -Command "containerapp job create --name $SeederJobName --resource-group $ResourceGroup --environment $AcaEnvName --image $SeederImage --registry-server $AcrLoginServer --registry-username $AcrUsername --registry-password $AcrPassword --trigger-type Manual --secrets $SeederSecrets --env-vars $SeederEnvVars --cpu 0.5 --memory 1Gi --output none" `
    -Description "Creazione ACA Job: $SeederJobName"

# Esegui il seeder
Write-Info "Avvio seeder (potrebbero volerci alcuni minuti per TMDB)..."
if (-not $WhatIf) {
    $jobResult = az containerapp job start --name $SeederJobName --resource-group $ResourceGroup --output none 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-OK "Seeder avviato. Verifica con:"
        Write-Info "  az containerapp job execution list -g $ResourceGroup -n $SeederJobName -o table"
        Write-Info "  az containerapp job logs show -n $SeederJobName -g $ResourceGroup --follow"
    }
    else {
        Write-Warn "Avvio seeder: $jobResult"
        Write-Info "Riprova manualmente: az containerapp job start -n $SeederJobName -g $ResourceGroup"
    }
}
else {
    Write-Info "[WHATIF] Esecuzione seeder saltata"
}

# ------ 9. DEPLOY FRONTEND ------
Write-Step "9. Deploy Frontend"

$WebImage = "${AcrLoginServer}/cinebase-web:latest"

# Determina URL frontend
if ([string]::IsNullOrWhiteSpace($FrontendExternalUrl)) {
    $WebFqdn = az containerapp show --name $WebAppName --resource-group $ResourceGroup --query properties.configuration.ingress.fqdn --output tsv 2>$null
    if ($WebFqdn) {
        $FrontendExternalUrl = "https://${WebFqdn}"
    }
    else {
        $FrontendExternalUrl = "https://PLACEHOLDER"  # Verrà aggiornato dopo il deploy
    }
}

$BackendInternalUrl = "http://${ApiAppName}:8080"
$WebEnvVars = "ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://+:8080 BACKEND_API_URL=${BackendInternalUrl}"

Write-Info "Creazione Container App: $WebAppName (Frontend)..."
Invoke-AzCommand -Command "containerapp create --name $WebAppName --resource-group $ResourceGroup --environment $AcaEnvName --image $WebImage --registry-server $AcrLoginServer --registry-username $AcrUsername --registry-password $AcrPassword --min-replicas 0 --max-replicas 3 --secrets azure-storage-account-key=$StorageAccountKey --env-vars $WebEnvVars --target-port 8080 --ingress external --enable-session-affinity --cpu 0.5 --memory 1Gi --output none" `
    -Description "Creazione Container App: $WebAppName (Frontend)"

# Monta volume Azure Files per Data Protection Keys (scaling orizzontale)
if (-not $WhatIf) {
    Write-Info "Montaggio volume Azure Files per Data Protection Keys..."
    $dpVolumeJs = "{`"name`":`"dataprotection`",`"storageName`":`"$DataProtectionShareName`",`"storageType`":`"AzureFile`"}"
    $dpMountJs = "{`"volumeName`":`"dataprotection`",`"mountPath`":`"/mnt/dataprotectionkeys`"}"
    $dpResult = az containerapp update --name $WebAppName --resource-group $ResourceGroup `
        --set "template.volumes=[$dpVolumeJs]" `
        --set "template.containers[0].volumeMounts=[$dpMountJs]" `
        --output none 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Warn "Montaggio volume Data Protection: $dpResult" }
}

# Ottieni URL pubblico
$WebFqdn = az containerapp show --name $WebAppName --resource-group $ResourceGroup --query properties.configuration.ingress.fqdn --output tsv
if ($WebFqdn) {
    $WebUrl = "https://${WebFqdn}"
}
else {
    $WebUrl = $FrontendExternalUrl
}
Write-OK "Frontend deployato: $WebUrl"

# ------ 10. POST-DEPLOY CONFIG ------
Write-Step "10. Post-deploy: aggiornamento CORS e OAuth"

# Aggiorna backend con URL frontend corretto
if (-not $WhatIf -and $WebFqdn) {
    Write-Info "Aggiornamento CORS e FRONTEND_BASE_URL sul backend..."
    $updateResult = az containerapp update --name $ApiAppName --resource-group $ResourceGroup `
        --set "environmentVariables[?name=='ALLOWED_CORS_ORIGINS'].value=https://$WebFqdn,http://localhost:5001" `
        --set "environmentVariables[?name=='FRONTEND_BASE_URL'].value=https://$WebFqdn" `
        --output none 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-OK "Backend aggiornato con URL frontend: https://$WebFqdn"
    }
    else {
        Write-Warn "Aggiornamento backend: $updateResult"
    }
}

Write-Info ""
Write-Info "Per OAuth con Google/Microsoft, aggiorna i redirect URI con:"
Write-Info "  https://${WebFqdn}/signin-google"
Write-Info "  https://${WebFqdn}/signin-microsoft"
Write-Info ""

# ------ RIEPILOGO ------
Write-Step "Riepilogo deploy"
Write-Info "Resource Group:       $ResourceGroup"
Write-Info "Location:             $Location"
Write-Info "ACR:                  $AcrLoginServer"
Write-Info "ACA Environment:      $AcaEnvName"
Write-Info "Storage Account:      $StorageAccountName"
Write-Info ""
Write-Info "Servizi deployati:"
Write-Info "  MariaDB (int):      $DbAppName (porta 3306)"
Write-Info "  Backend API (int):  $ApiAppName (porta 8080)"
Write-Info "  Frontend (ext):     $WebUrl"
Write-Info "  Seeder (Job):       $SeederJobName"
Write-Info ""
Write-Info "Credenziali admin:"
Write-Info "  Email:              $AdminEmail"
Write-Info "  Password:           $AdminPassword (salvala!)"
Write-Info ""
Write-Info "Comandi utili:"
Write-Info "  Log backend:        az containerapp logs show -n $ApiAppName -g $ResourceGroup --follow"
Write-Info "  Log frontend:       az containerapp logs show -n $WebAppName -g $ResourceGroup --follow"
Write-Info "  Esegui seeder:      az containerapp job start -n $SeederJobName -g $ResourceGroup"
Write-Info "  Log seeder:         az containerapp job execution list -n $SeederJobName -g $ResourceGroup -o table"
Write-Info "  Elimina tutto:      az group delete -n $ResourceGroup --yes --no-wait"
