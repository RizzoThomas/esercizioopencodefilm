// =============================================================================
// CineBase — Azure Container Apps Bicep Template
// =============================================================================
// Deploy: az deployment group create --resource-group rg-cinebase-prod \
//         --template-file infra/azure/main.bicep --parameters infra/azure/parameters.json
// =============================================================================

targetScope = 'resourceGroup'

// ─── PARAMETERS ────────────────────────────────────────────────────────────
@description('Azure location for all resources')
param location string = 'italynorth'

@description('Name for the Container Registry')
param acrName string = 'acrcinebase${uniqueString(resourceGroup().id)}'

@description('ACA Environment name')
param acaEnvName string = 'aca-env-cinebase'

@description('MariaDB root password (provide via Key Vault or parameter file)')
@secure()
param mariadbRootPassword string

@description('JWT signing secret (min 32 chars)')
@secure()
param jwtSecret string

@description('TMDB Bearer Token for film seeding')
@secure()
param tmdbBearerToken string

@description('Storage Account SKU')
param storageSku string = 'Standard_LRS'

@description('Admin seed email')
param adminSeedEmail string = 'admin@cinebase.it'

@description('Admin seed password')
@secure()
param adminSeedPassword string

// ─── RESOURCES ─────────────────────────────────────────────────────────────

// Log Analytics Workspace
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'la-cinebase-${uniqueString(resourceGroup().id)}'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
  }
}

// ACA Environment
resource acaEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: acaEnvName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// Storage Account
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'stcinebase${uniqueString(resourceGroup().id)}'
  location: location
  kind: 'StorageV2'
  sku: { name: storageSku }
}

// File Share for MariaDB
resource mariadbFileShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-01-01' = {
  name: '${storageAccount.name}/default/mariadb-data'
  properties: { shareQuota: 5 }
}

// File Share for Data Protection Keys
resource dataprotectionFileShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-01-01' = {
  name: '${storageAccount.name}/default/web-dataprotection-keys'
  properties: { shareQuota: 1 }
}

// Container App: MariaDB
resource mariadbApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'cinebase-db'
  location: location
  properties: {
    environmentId: acaEnvironment.id
    configuration: {
      ingress: {
        external: false
        targetPort: 3306
        transport: 'tcp'
      }
      secrets: [
        { name: 'mariadb-root-password', value: mariadbRootPassword }
      ]
      registries: []
    }
    template: {
      containers: [
        {
          image: 'mariadb:11.4'
          name: 'mariadb'
          env: [
            { name: 'MARIADB_ROOT_PASSWORD', secretRef: 'mariadb-root-password' }
            { name: 'MARIADB_DATABASE', value: 'film-api-db' }
          ]
          resources: { cpu: 0.5, memory: '1Gi' }
        }
      ]
      scale: { minReplicas: 0, maxReplicas: 1 }
    }
  }
}

// Container App: Backend API
resource backendApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'cinebase-api'
  location: location
  properties: {
    environmentId: acaEnvironment.id
    configuration: {
      ingress: {
        external: false
        targetPort: 8080
        transport: 'http'
      }
      secrets: [
        { name: 'mariadb-root-password', value: mariadbRootPassword }
        { name: 'jwt-secret', value: jwtSecret }
        { name: 'tmdb-bearer-token', value: tmdbBearerToken }
      ]
    }
    template: {
      containers: [
        {
          image: '${acrName}.azurecr.io/cinebase-api:latest'
          name: 'cinebase-api'
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'ASPNETCORE_URLS', value: 'http://+:8080' }
            { name: 'DB_HOST', value: 'cinebase-db' }
            { name: 'DB_PORT', value: '3306' }
            { name: 'DB_NAME', value: 'film-api-db' }
            { name: 'DB_USER', value: 'root' }
            { name: 'DB_PASSWORD', secretRef: 'mariadb-root-password' }
            { name: 'JWT_SECRET', secretRef: 'jwt-secret' }
            { name: 'JWT_ISSUER', value: 'CineBaseAPI' }
            { name: 'JWT_AUDIENCE', value: 'CineBaseWeb' }
            { name: 'TMDB_BEARER_TOKEN', secretRef: 'tmdb-bearer-token' }
            { name: 'ADMIN_SEED_EMAIL', value: adminSeedEmail }
            { name: 'ADMIN_SEED_PASSWORD', value: adminSeedPassword }
          ]
          resources: { cpu: 0.5, memory: '1Gi' }
          livenessProbe: {
            kind: 'HTTP'
            path: '/health'
            port: 8080
            initialDelaySeconds: 30
            periodSeconds: 30
          }
          readinessProbe: {
            kind: 'HTTP'
            path: '/health'
            port: 8080
            initialDelaySeconds: 15
            periodSeconds: 15
          }
        }
      ]
      scale: { minReplicas: 0, maxReplicas: 3 }
    }
  }
}

// Container App: Frontend
resource frontendApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'cinebase-web'
  location: location
  properties: {
    environmentId: acaEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        stickySessions: { affinity: 'sticky' }
      }
      secrets: []
    }
    template: {
      containers: [
        {
          image: '${acrName}.azurecr.io/cinebase-web:latest'
          name: 'cinebase-web'
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'ASPNETCORE_URLS', value: 'http://+:8080' }
            { name: 'BACKEND_API_URL', value: 'http://cinebase-api:8080' }
          ]
          resources: { cpu: 0.5, memory: '1Gi' }
        }
      ]
      scale: { minReplicas: 0, maxReplicas: 3 }
    }
  }
}

// Container App Job: Seeder
resource seederJob 'Microsoft.App/jobs@2024-03-01' = {
  name: 'cinebase-seeder'
  location: location
  properties: {
    environmentId: acaEnvironment.id
    configuration: {
      triggerType: 'Manual'
      secrets: [
        { name: 'mariadb-root-password', value: mariadbRootPassword }
        { name: 'tmdb-bearer-token', value: tmdbBearerToken }
      ]
      registries: []
    }
    template: {
      containers: [
        {
          image: '${acrName}.azurecr.io/cinebase-seeder:latest'
          name: 'cinebase-seeder'
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'DB_HOST', value: 'cinebase-db' }
            { name: 'DB_PORT', value: '3306' }
            { name: 'DB_NAME', value: 'film-api-db' }
            { name: 'DB_USER', value: 'root' }
            { name: 'DB_PASSWORD', secretRef: 'mariadb-root-password' }
            { name: 'TMDB_BEARER_TOKEN', secretRef: 'tmdb-bearer-token' }
            { name: 'ADMIN_SEED_EMAIL', value: adminSeedEmail }
            { name: 'ADMIN_SEED_PASSWORD', value: adminSeedPassword }
          ]
          resources: { cpu: 0.5, memory: '1Gi' }
        }
      ]
    }
  }
}

// ─── OUTPUTS ───────────────────────────────────────────────────────────────
output acrLoginServer string = '${acrName}.azurecr.io'
output acaEnvironmentName string = acaEnvironment.name
output mariadbAppName string = mariadbApp.name
output backendAppName string = backendApp.name
output frontendAppUrl string = 'https://${frontendApp.properties.configuration.ingress.fqdn}'
output seederJobName string = seederJob.name
output storageAccountName string = storageAccount.name
