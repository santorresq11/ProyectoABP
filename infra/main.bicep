// =====================================================================================
// AulaNube - Infraestructura como código (Azure Bicep)
// Plataforma SaaS de alerta temprana de deserción escolar para colegios sin
// infraestructura tecnológica propia. Un solo despliegue atiende a muchos colegios
// (multi-inquilino), así ningún colegio necesita servidores ni personal de TI.
//
// Despliegue:
//   az group create -n rg-aulanube-dev -l eastus2
//   az deployment group create -g rg-aulanube-dev -f infra/main.bicep -p infra/main.parameters.json \
//      -p sqlAdminPassword='<contraseña segura>'
// =====================================================================================

targetScope = 'resourceGroup'

@description('Prefijo corto para nombrar los recursos.')
@minLength(3)
@maxLength(10)
param prefijo string = 'aulanube'

@description('Ambiente: dev, test o prod.')
@allowed(['dev', 'test', 'prod'])
param ambiente string = 'dev'

@description('Región principal de Azure.')
param ubicacion string = resourceGroup().location

@description('Región para Static Web Apps (no está disponible en todas las regiones).')
param ubicacionWeb string = 'eastus2'

@description('Usuario administrador de Azure SQL (solo para emergencias; la API usa identidad administrada).')
param sqlAdminUsuario string = 'aulanubeadmin'

@secure()
@description('Contraseña del administrador de Azure SQL.')
param sqlAdminPassword string

@description('Desplegar el área de trabajo de Azure Machine Learning (entrenamiento del modelo LSTM).')
param desplegarMachineLearning bool = true

var sufijo = uniqueString(resourceGroup().id)
var nombreBase = '${prefijo}-${ambiente}'
var etiquetas = {
  proyecto: 'AulaNube'
  ambiente: ambiente
  curso: 'Taller ABP - IA en la organizacion'
}

// ---------- Observabilidad ----------
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${nombreBase}'
  location: ubicacion
  tags: etiquetas
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${nombreBase}'
  location: ubicacion
  kind: 'web'
  tags: etiquetas
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// ---------- Secretos ----------
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: take('kv-${prefijo}-${sufijo}', 24)
  location: ubicacion
  tags: etiquetas
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

// ---------- Almacenamiento (artefactos del modelo y archivos de carga masiva) ----------
resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: take('st${prefijo}${sufijo}', 24)
  location: ubicacion
  tags: etiquetas
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
}

resource contenedorModelos 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'modelos'
}

resource contenedorCargas 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'cargas-simat'
}

// ---------- Base de datos (Azure SQL serverless: se pausa sola cuando no hay uso) ----------
resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: 'sql-${nombreBase}-${sufijo}'
  location: ubicacion
  tags: etiquetas
  properties: {
    administratorLogin: sqlAdminUsuario
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlDb 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: 'sqldb-aulanube'
  location: ubicacion
  tags: etiquetas
  sku: {
    name: 'GP_S_Gen5_1'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    autoPauseDelay: 60
    minCapacity: json('0.5')
    maxSizeBytes: 34359738368
  }
}

resource sqlFirewallAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'PermitirServiciosAzure'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ---------- Backend .NET 8 (Azure App Service Linux) ----------
resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'asp-${nombreBase}'
  location: ubicacion
  tags: etiquetas
  kind: 'linux'
  sku: {
    name: ambiente == 'prod' ? 'P0v3' : 'B1'
  }
  properties: {
    reserved: true
  }
}

resource api 'Microsoft.Web/sites@2023-12-01' = {
  name: 'app-${nombreBase}-api-${sufijo}'
  location: ubicacion
  tags: etiquetas
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: ambiente == 'prod'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      healthCheckPath: '/health'
      appSettings: [
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
        { name: 'ASPNETCORE_ENVIRONMENT', value: ambiente == 'prod' ? 'Production' : 'Development' }
        { name: 'Cors__Origenes__0', value: 'https://${web.properties.defaultHostname}' }
        { name: 'Almacenamiento__Url', value: storage.properties.primaryEndpoints.blob }
        { name: 'KeyVault__Url', value: keyVault.properties.vaultUri }
      ]
      connectionStrings: [
        {
          name: 'AulaNube'
          type: 'SQLAzure'
          connectionString: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${sqlDb.name};Authentication=Active Directory Default;Encrypt=True;'
        }
      ]
    }
  }
}

// ---------- Frontend Angular (Azure Static Web Apps) ----------
resource web 'Microsoft.Web/staticSites@2023-12-01' = {
  name: 'stapp-${nombreBase}'
  location: ubicacionWeb
  tags: etiquetas
  sku: {
    name: ambiente == 'prod' ? 'Standard' : 'Free'
    tier: ambiente == 'prod' ? 'Standard' : 'Free'
  }
  properties: {}
}

// ---------- Roles: la API lee modelos (Blob) y secretos (Key Vault) con identidad administrada ----------
var rolLectorBlob = '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1' // Storage Blob Data Reader
var rolLectorSecretos = '4633458b-17de-408a-b874-0445c86b69e6' // Key Vault Secrets User

resource apiLeeBlobs 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, api.id, rolLectorBlob)
  scope: storage
  properties: {
    principalId: api.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', rolLectorBlob)
  }
}

resource apiLeeSecretos 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, api.id, rolLectorSecretos)
  scope: keyVault
  properties: {
    principalId: api.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', rolLectorSecretos)
  }
}

// ---------- IA: Azure Machine Learning (entrenamiento LSTM y registro de modelos) ----------
resource mlWorkspace 'Microsoft.MachineLearningServices/workspaces@2024-04-01' = if (desplegarMachineLearning) {
  name: 'mlw-${nombreBase}'
  location: ubicacion
  tags: etiquetas
  identity: { type: 'SystemAssigned' }
  sku: { name: 'Basic', tier: 'Basic' }
  properties: {
    friendlyName: 'AulaNube - Alerta temprana de desercion'
    storageAccount: storage.id
    keyVault: keyVault.id
    applicationInsights: appInsights.id
    publicNetworkAccess: 'Enabled'
  }
}

output apiUrl string = 'https://${api.properties.defaultHostName}'
output webUrl string = 'https://${web.properties.defaultHostname}'
output apiNombre string = api.name
output staticWebAppNombre string = web.name
output sqlServidor string = sqlServer.properties.fullyQualifiedDomainName
output almacenamiento string = storage.name
