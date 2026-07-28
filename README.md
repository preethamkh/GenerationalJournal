# Generational Journal

A multi-generational family journal application built with .NET 9 and Clean Architecture.

## Architecture

- **Domain** - Entities, repository interfaces, business logic
- **Application** - DTOs, services, business rules
- **Infrastructure** - EF Core, database (SQLite), repository implementations
- **API** - Minimal API endpoints, authentication, middleware

## Tech Stack

- .NET 9 Minimal API
- SQLite (portable single file database)
- Entity Framework Core
- JWT Authentication
- Serilog Structured Logging
- OpenAPI / Swagger

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

## Project Structure

```
GenerationalJournal/
├── GenerationalJournal.Domain/       # Entities, interfaces
├── GenerationalJournal.Application/  # Services, DTOs
├── GenerationalJournal.Infrastructure/ # EF Core, repositories
├── GenerationalJournal.Api/          # Minimal API, configuration
├── data/                             # SQLite database (created at runtime)
└── docs/                             # Documentation
```
