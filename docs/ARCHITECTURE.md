# Architecture

## Clean Architecture Overview

The Generational Journal application follows Clean Architecture principles with four distinct layers.

### Layer Dependencies

```
API → Infrastructure → Application → Domain
```

Each layer only depends on the layers to its right.

### Domain Layer (`GenerationalJournal.Domain`)

The innermost layer containing enterprise business rules.

- **Entities**: `User`, `Family`, `FamilyMember`, `JournalEntry`
- **Repository Interfaces**: `IUserRepository`, `IFamilyRepository`, `IJournalEntryRepository`

Has no dependencies on other projects or external frameworks.

### Application Layer (`GenerationalJournal.Application`)

Contains application-specific business rules and orchestration.

- **DTOs**: Request/response objects for API communication
- **Services**: `IAuthService`, `IFamilyService`, `IJournalService` and their implementations

Depends only on the Domain layer.

### Infrastructure Layer (`GenerationalJournal.Infrastructure`)

Implements interfaces defined in the Domain layer using external frameworks.

- **Data**: `AppDbContext` (EF Core with SQLite)
- **Repositories**: `UserRepository`, `FamilyRepository`, `JournalEntryRepository`

Depends on Domain and Application layers.

### API Layer (`GenerationalJournal.Api`)

The outermost layer providing REST endpoints via .NET 9 Minimal APIs.

- **Endpoints**: Auth, Family, Journal routes
- **Middleware**: Global error handling, Serilog request logging
- **Configuration**: JWT auth, Swagger/OpenAPI, CORS, health checks

Depends on all inner layers.

## Database

SQLite is used as a portable, single-file database stored at `../data/familyjournal.db` relative to the API project. The database and schema are created automatically on first run via `EnsureCreated()`.

## Authentication

JWT Bearer token authentication secures all API endpoints except:
- `GET /` (root info)
- `GET /health` (health check)
- `POST /api/auth/register` (registration)
- `POST /api/auth/login` (login)

Tokens are signed with a symmetric key configured in `appsettings.json`.
