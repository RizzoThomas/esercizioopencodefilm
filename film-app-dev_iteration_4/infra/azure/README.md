# Deploy CineBase su Azure Container Apps (ACA)

## Prerequisiti

- **Account Azure** con sottoscrizione attiva (Azure for Students funziona)
- **Azure CLI** installata e autenticata (`az login`)
- **Docker Desktop** (opzionale — si può usare `az acr build` senza Docker locale)
- **TMDB Bearer Token** da [https://www.themoviedb.org/settings/api](https://www.themoviedb.org/settings/api)
- Il codice deve essere già stato patchato per ACA (CORS e BACKEND_API_URL middleware)

## Opzione A: Script PowerShell (consigliato per deploy rapido)

```powershell
# Apri PowerShell come amministratore
cd CineBase

# Esegui lo script con il TMDB token
.\infra\azure\aca-deploy.ps1 -TmdbToken "eyJhbGciOiJIUzI1NiJ9..."
```

Parametri opzionali:

| Parametro | Default | Descrizione |
|-----------|---------|-------------|
| `-ResourceGroup` | `rg-cinebase-prod` | Nome resource group |
| `-Location` | `italynorth` | Regione Azure |
| `-AcrName` | `acrcinebase<random>` | Nome Container Registry |
| `-AcaEnvName` | `aca-env-cinebase` | Nome ACA Environment |
| `-DbPassword` | auto-generato | Password root MariaDB |
| `-JwtSecret` | auto-generato (64 char) | Chiave firma JWT |
| `-AdminEmail` | `admin@cinebase.it` | Email admin seed |
| `-AdminPassword` | auto-generato | Password admin seed |
| `-FrontendExternalUrl` | auto-letto | URL frontend (se noto) |
| `-WhatIf` | - | Dry-run senza eseguire |

Esempio con parametri personalizzati:
```powershell
.\infra\azure\aca-deploy.ps1 -ResourceGroup "rg-cinebase-test" -Location "westeurope" -TmdbToken "eyJ..."
```

## Opzione B: Bicep Template (per CI/CD e deploy ripetibili)

```powershell
# 1. Crea il resource group (se non esiste)
az group create --name rg-cinebase-prod --location italynorth

# 2. Crea parameter file con i tuoi valori (modifica infra/azure/parameters.json)
# Inserisci mariadbRootPassword, jwtSecret, tmdbBearerToken, adminSeedPassword

# 3. Deploy con Bicep
az deployment group create --resource-group rg-cinebase-prod `
    --template-file infra/azure/main.bicep `
    --parameters infra/azure/parameters.json
```

## Opzione C: Passo-Passo Manuale

Segui la guida dettagliata in `ACA-DEPLOY-GUIDE.md`.

## Post-Deploy

1. **Ottieni URL frontend**:
   ```powershell
   az containerapp show --name cinebase-web -g rg-cinebase-prod --query properties.configuration.ingress.fqdn -o tsv
   ```

2. **Esegui il seeder** (se non già eseguito automaticamente):
   ```powershell
   az containerapp job start --name cinebase-seeder -g rg-cinebase-prod
   ```

3. **Configura OAuth**: aggiorna i redirect URI su Google Cloud Console e Microsoft Entra ID con l'URL del frontend.

4. **Configura dominio personalizzato** (opzionale):
   ```powershell
   az containerapp hostname add --name cinebase-web -g rg-cinebase-prod --hostname www.tuodominio.it
   # Poi configura il certificato gestito da ACA
   ```

## Costi Stimati (italynorth, SKU Basic/Consumption)

| Servizio | Costo/giorno | Note |
|----------|-------------|------|
| ACR Basic | ~€0.17 | Fisso |
| ACA Environment | ~€0.00 | Incluso nel consumo |
| MariaDB (min 0) | ~€0.00 | Solo quando usato |
| Backend (min 0) | ~€0.00 | Solo quando usato |
| Frontend (min 0) | ~€0.00 | Solo quando usato |
| Storage (5GB LRS) | ~€0.01 | Fisso |
| **Totale** | **~€0.20/giorno** | **~€6/mese** |

## Troubleshooting

| Problema | Soluzione |
|----------|-----------|
| `az acr build` fallisce | Usa `az acr build` con `--no-logs` o builda localmente con Docker |
| Volume Azure Files non si monta | Aggiorna ACA extension: `az extension update --name containerapp` |
| Seeder non riesce a connettersi al DB | Verifica che MariaDB sia running. Il seeder ha retry logic integrato (5 tentativi) |
| CORS blocca le richieste frontend | Verifica ALLOWED_CORS_ORIGINS sul backend includa l'URL ACA del frontend |
| Cold start lento | Imposta `--min-replicas 1` invece di `0` per mantenere un'istanza sempre attiva |
