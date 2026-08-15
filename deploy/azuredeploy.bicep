@description('Base name for the deployed resources. Lowercase alphanumeric and hyphens only.')
@minLength(3)
@maxLength(24)
param appName string

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Connection string for the SQLite database used by the backup function.')
@secure()
param databaseConnectionString string = ''

@description('Public URL of the API health endpoint to monitor.')
param apiHealthUrl string = ''

var storageAccountName = toLower('st${appName}')
var functionAppName = toLower('func${appName}')
var appServicePlanName = 'asp-${appName}'
var appInsightsName = 'appi-${appName}'
var databaseBackupContainer = 'database-backups'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {}
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
  }
}

resource functionAppSettings 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: functionApp
  name: 'appsettings'
  properties: {
    AzureWebJobsStorage: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
    FUNCTIONS_WORKER_RUNTIME: 'dotnet-isolated'
    FUNCTIONS_EXTENSION_VERSION: '~4'
    WEBSITE_RUN_FROM_PACKAGE: '1'
    APPLICATIONINSIGHTS_CONNECTION_STRING: appInsights.properties.ConnectionString
    'ConnectionStrings:DefaultConnection': databaseConnectionString
    'DatabaseBackup:Container': databaseBackupContainer
    'HealthCheck:ApiUrl': apiHealthUrl
  }
}

output functionAppName string = functionApp.name
output storageAccountName string = storageAccount.name
output functionAppResourceId string = functionApp.id
