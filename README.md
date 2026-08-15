# Generational Journal

A multi-generational family journal application built with .NET 9 and Clean Architecture.

## Architecture

- **Domain** - Entities, repository interfaces, business logic
- **Application** - DTOs, services, business rules
- **Infrastructure** - EF Core, database (SQLite), repository implementations
- **API** - Minimal API endpoints, authentication, middleware
- **Web** - Blazor Web App frontend (Interactive Server, Bootstrap)
- **Functions** - Azure Functions (thumbnail generation, database backup, health monitoring)

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

## Quick Start

### Prerequisites

- .NET 9 SDK or later

### Run the API

```bash
cd GenerationalJournal.Api
dotnet run
```

The API starts at `http://localhost:5278` (or the port shown in the console).

### API Documentation

Once running, open `http://localhost:5278/swagger` for the Swagger UI.

### Health Check

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

## Project Structure

```
GenerationalJournal/
├── GenerationalJournal.Domain/       # Entities, interfaces
├── GenerationalJournal.Application/  # Services, DTOs
├── GenerationalJournal.Infrastructure/ # EF Core, repositories
├── GenerationalJournal.Api/          # Minimal API, configuration
├── GenerationalJournal.Web/          # Blazor Web App frontend
├── GenerationalJournal.Functions/    # Azure Functions (thumbnails, backup, health)
├── data/                             # SQLite database (created at runtime)
├── deploy/                           # Azure Bicep deployment templates
├── scripts/                          # Local development helper scripts
└── docs/                             # Documentation
```
