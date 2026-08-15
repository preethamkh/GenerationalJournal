# Azure Functions Integration

The `GenerationalJournal.Functions` project is a .NET 9 isolated-worker Azure
Functions app that offloads background work from the API. It contains three
functions:

| Function | Trigger | Schedule | Purpose |
| --- | --- | --- | --- |
| `ThumbnailFunction` | Blob (`media/{familyId}/{entryId}/{fileName}`) | On new image | Generates a 320px thumbnail and writes it to `thumbnails/{familyId}/{entryId}/{fileName}` |
| `DatabaseBackupFunction` | Timer | `0 0 2 * * *` (daily 02:00 UTC) | Copies the SQLite database to the `database-backups` blob container using `VACUUM INTO` |
| `HealthCheckFunction` | Timer | `0 */5 * * * *` (every 5 minutes) | Pings the API `/health` endpoint and logs the result |

## Configuration

Settings live in `GenerationalJournal.Functions/local.settings.json` for local
development and in the Azure Function App settings when deployed.

| Key | Default | Description |
| --- | --- | --- |
| `AzureWebJobsStorage` | `UseDevelopmentStorage=true` | Storage account for triggers and blobs (Azurite locally) |
| `FUNCTIONS_WORKER_RUNTIME` | `dotnet-isolated` | Isolated worker runtime |
| `ConnectionStrings:DefaultConnection` | `Data Source=../data/familyjournal.db` | SQLite connection string used by the backup function |
| `DatabaseBackup:Container` | `database-backups` | Blob container that receives database backups |
| `HealthCheck:ApiUrl` | `http://localhost:5278/health` | API health endpoint monitored by the health check function |

## Local Development with Azurite

The Functions host uses the Azurite storage emulator for blob triggers and
outputs. Azurite exposes Blob on port 10000, Queue on 10001, and Table on 10002;
`UseDevelopmentStorage=true` points at these default endpoints.

### Prerequisites

```bash
npm install -g azurite
npm install -g azure-functions-core-tools@4
```

### Run

1. Start the API (so the health check and database backup targets exist):

   ```bash
   dotnet run --project GenerationalJournal.Api
   ```

2. In a second terminal, start Azurite:

   ```powershell
   ./scripts/start-azurite.ps1
   ```

3. In a third terminal, start the Functions host:

   ```powershell
   ./scripts/run-functions.ps1
   ```

### Testing the thumbnail function

Upload an image to the `media` container in Azurite (for example using
Azure Storage Explorer pointed at `http://127.0.0.1:10000` with the
development account, path `media/{familyId}/{entryId}/{fileName}`). The
function generates a thumbnail into `thumbnails/{familyId}/{entryId}/{fileName}`.

## Deployment to Azure

Infrastructure is defined as Bicep in `deploy/azuredeploy.bicep` and creates a
Storage account, Consumption-plan Function App, and Application Insights.

```bash
az group create --name rg-generationaljournal --location eastus
az deployment group create \
  --resource-group rg-generationaljournal \
  --template-file deploy/azuredeploy.bicep \
  --parameters deploy/azuredeploy.parameters.json
```

The Function App itself is published via the
`.github/workflows/deploy-functions.yml` workflow (manual `workflow_dispatch`
trigger), which builds, publishes, and deploys using
`Azure/functions-action`. Configure the `AZURE_FUNCTIONAPP_PUBLISH_PROFILE`
repository secret with the Function App's publish profile before running it.
