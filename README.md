# Generational Journal

A multi-generational family journal application built with .NET 9 and Clean Architecture.

## Architecture

- **Domain** - Entities, repository interfaces, business logic
- **Application** - DTOs, services, business rules
- **Infrastructure** - EF Core, database (SQLite), repository implementations
- **API** - Minimal API endpoints, authentication, middleware
- **Web** - Blazor Web App frontend (Interactive Server, Bootstrap)
- **Functions** - Azure Functions (thumbnail generation, database backup, health monitoring)

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for details on the storage
abstraction and media management.

## Tech Stack

- .NET 9 Minimal API
- .NET 9 Blazor Web App (Interactive Server render mode)
- SQLite (portable single file database)
- Entity Framework Core
- JWT Authentication
- Serilog Structured Logging
- OpenAPI / Swagger
- Bootstrap 5
- Azure Functions (.NET 9 isolated worker) + Azurite emulator

## Setup

### Prerequisites

- .NET 9 SDK or later

#### Windows

1. Install the .NET 9 SDK from https://dotnet.microsoft.com/download
   (or `winget install Microsoft.DotNet.SDK.9`).
2. Verify the install:

   ```powershell
   dotnet --version
   ```

#### macOS

1. Install the .NET 9 SDK either from https://dotnet.microsoft.com/download or via Homebrew:

   ```bash
   brew install --cask dotnet-sdk
   ```

2. Verify the install:

   ```bash
   dotnet --version
   ```

### Clone and build

```bash
git clone <repository-url>
cd GenerationalJournal
dotnet build GenerationalJournal.slnx
```

### Run the API

The API creates the SQLite database at `data/familyjournal.db` and the media
folder at `data/media/` automatically on first run.

```bash
cd GenerationalJournal.Api
dotnet run
```

The API starts at `http://localhost:5278` (or the port shown in the console).

#### API Documentation

Once running, open `http://localhost:5278/swagger` for the Swagger UI.

#### Health Check

```bash
curl http://localhost:5278/health
```

### Run the Web App

The frontend calls the API over HTTP, so start the API first, then run the Blazor app:

```bash
cd GenerationalJournal.Api
dotnet run
```

In a second terminal:

```bash
cd GenerationalJournal.Web
dotnet run
```

The web app starts at `http://localhost:5037` (or the port shown in the console). The
API base URL is configurable via the `ApiBaseUrl` setting in
`GenerationalJournal.Web/appsettings.json` (defaults to `http://localhost:5278`).

### Azure Functions

Background jobs (image thumbnails, database backups, health monitoring) run as
Azure Functions. See [docs/AZURE_FUNCTIONS.md](docs/AZURE_FUNCTIONS.md) for
local setup with Azurite and deployment instructions.

## Backups

The SQLite database and uploaded media live in the `data/` directory, which is
gitignored and not part of the repository. Back them up separately.

### Windows

Use the PowerShell backup script, which copies the database and media to a NAS
share or external drive with `robocopy`:

```powershell
# Timestamped snapshot (keeps 30 days by default)
.\scripts\backup.ps1 -Destination "\\nas\family-backups"

# Mirror directly into a fixed NAS share (drops deleted files)
.\scripts\backup.ps1 -Destination "D:\backups\journal" -Mirror

# External drive, keep only 14 days of snapshots
.\scripts\backup.ps1 -Destination "E:\backups" -RetainDays 14
```

Schedule it with Task Scheduler (daily, e.g. 03:00) for automated backups, or
use the [GitHub Actions backup workflow](#automated-backup).

### macOS / Linux

`robocopy` is Windows-only. Use `rsync` for the equivalent:

```bash
rsync -av --delete data/media/ /Volumes/Backup/journal/media/
cp data/familyjournal.db* /Volumes/Backup/journal/database/
```

Schedule it with `crontab -e`:

```cron
0 3 * * * rsync -av --delete /path/to/GenerationalJournal/data/ /Volumes/Backup/journal/
```

### Automated backup

`.github/workflows/backup.yml` runs a scheduled backup that copies the database
and media to a remote host over SSH. Configure the `BACKUP_HOST`,
`BACKUP_USER`, and `BACKUP_SSH_KEY` repository secrets and adjust the target
path to enable it.

## Deployment

See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for cloud hosting options (Azure,
Oracle Cloud Free Tier, Backblaze B2, AWS) and the SQLite-to-PostgreSQL
migration guide.

## Project Structure

```
GenerationalJournal/
├── GenerationalJournal.Domain/       # Entities, interfaces
├── GenerationalJournal.Application/  # Services, DTOs
├── GenerationalJournal.Infrastructure/ # EF Core, repositories, storage
├── GenerationalJournal.Api/          # Minimal API, configuration
├── GenerationalJournal.Web/          # Blazor Web App frontend
├── GenerationalJournal.Functions/    # Azure Functions (thumbnails, backup, health)
├── data/                             # SQLite database + media (created at runtime, gitignored)
├── deploy/                           # Azure Bicep deployment templates
├── scripts/                          # Local development + backup helper scripts
├── .github/workflows/                # CI, deploy, and backup workflows
└── docs/                             # Documentation
```
