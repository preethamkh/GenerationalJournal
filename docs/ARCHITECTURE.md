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

- **Entities**: `User`, `Family`, `FamilyMember`, `JournalEntry`, `MediaItem`
- **Repository Interfaces**: `IUserRepository`, `IFamilyRepository`, `IJournalEntryRepository`, `IMediaRepository`

Has no dependencies on other projects or external frameworks.

### Application Layer (`GenerationalJournal.Application`)

Contains application-specific business rules and orchestration.

- **DTOs**: Request/response objects for API communication
- **Services**: `IAuthService`, `IFamilyService`, `IJournalService`, `IMediaService` and their implementations
- **Storage Abstraction**: `IMediaStorage` interface (see [Storage Abstraction](#storage-abstraction))
- **Configuration**: `JwtSettings`, `MediaSettings` options classes

Depends only on the Domain layer.

### Infrastructure Layer (`GenerationalJournal.Infrastructure`)

Implements interfaces defined in the Domain and Application layers using external frameworks.

- **Data**: `AppDbContext` (EF Core with SQLite)
- **Repositories**: `UserRepository`, `FamilyRepository`, `JournalEntryRepository`, `MediaRepository`
- **Storage**: `LocalFileSystemMediaStorage`, `CloudBlobMediaStorage`

Depends on Domain and Application layers.

### API Layer (`GenerationalJournal.Api`)

The outermost layer providing REST endpoints via .NET 9 Minimal APIs.

- **Endpoints**: Auth, Family, Journal, Media routes
- **Middleware**: Global error handling, Serilog request logging
- **Configuration**: JWT auth, Swagger/OpenAPI, CORS, health checks, media storage provider selection

Depends on all inner layers.

## Database

SQLite is used as a portable, single-file database stored at `../data/familyjournal.db` relative to the API project. The database and schema are created automatically on first run via `EnsureCreated()`.

The connection string is resolved in `Program.cs` from `ConnectionStrings:DefaultConnection` (defaulting to `Data Source=../data/familyjournal.db`). A migration path to PostgreSQL is documented in [DEPLOYMENT.md](DEPLOYMENT.md).

## Storage Abstraction

Media files are accessed through the `IMediaStorage` interface defined in the
Application layer, so the API is decoupled from where files physically live.

```csharp
public interface IMediaStorage
{
    Task<string> SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> GetAsync(string storagePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
    Task BackupAsync(string storagePath, CancellationToken cancellationToken = default);
}
```

### Implementations

| Provider | Class | Description |
| --- | --- | --- |
| `LocalFileSystem` (default) | `LocalFileSystemMediaStorage` | Stores files under a root directory on local disk (or a mounted external/NAS share). |
| `CloudBlob` | `CloudBlobMediaStorage` | Placeholder for Azure Blob Storage; methods currently throw `NotImplementedException`. |

### Provider selection

The active provider is chosen at startup from the `Media:StorageProvider`
configuration value in `Program.cs`:

- `LocalFileSystem` (default) registers `LocalFileSystemMediaStorage` with the
  resolved `MediaSettings`.
- `CloudBlob` registers `CloudBlobMediaStorage`.

The storage root path is resolved from `Media:StorageRootPath`. When empty or
relative, `Program.cs` normalizes it to an absolute path rooted at the content
directory (`../data/media` by default), so the media folder lives alongside the
SQLite database under `data/`.

## Media Management

Media (photos and videos) are attached to journal entries and stored through the
`IMediaStorage` abstraction.

### Data model

- **`MediaItem`** entity records metadata about an uploaded file and is persisted
  in the `MediaItems` table via EF Core.
- The actual bytes are stored by `IMediaStorage`, not in the database. The
  `StoragePath` column stores the provider-relative path returned by
  `SaveAsync`.

### Upload flow

`MediaService.UploadMediaAsync` performs the following validation and steps:

1. Loads the journal entry and verifies the caller is a member of its family.
2. Validates the file extension against `Media:AllowedExtensions`
   (`.jpg`, `.png`, `.gif`, `.mp4`, `.mov`) and size against
   `Media:MaxFileSizeBytes` (default 100 MB).
3. Classifies the file as `image` or `video` from its extension.
4. Generates a unique stored file name and a relative path of the form
   `{familyId}/{entryId}/{storedFileName}`.
5. Saves the bytes via `IMediaStorage.SaveAsync`.
6. Persists a `MediaItem` row referencing the returned storage path.

### Storage layout

Files are organized on disk (or in blob storage) by family and entry:

```
data/media/{familyId}/{entryId}/{storedFileName}
```

This keeps a family's media self-contained, simplifies backups of individual
families, and mirrors the container/folder layout the Azure Functions thumbnail
trigger expects (`media/{familyId}/{entryId}/{fileName}`).

### API surface

The media endpoints are mapped in `GenerationalJournal.Api/Endpoints/MediaEndpoints.cs`:

| Method | Route | Description |
| --- | --- | --- |
| `POST` | `/api/entries/{id}/media` | Upload a file to a journal entry |
| `GET` | `/api/entries/{id}/media` | List media for a journal entry |
| `GET` | `/api/media/{id}/file` | Download a media file |
| `DELETE` | `/api/media/{id}` | Delete a media item (removes metadata and file) |

### Configuration

Media behavior is configured through the `Media` section in `appsettings.json`
and bound to `MediaSettings`:

| Key | Default | Description |
| --- | --- | --- |
| `Media:StorageProvider` | `LocalFileSystem` | Which `IMediaStorage` implementation to use |
| `Media:StorageRootPath` | `../data/media/` | Root directory for local file storage |
| `Media:MaxFileSizeBytes` | `104857600` (100 MB) | Maximum allowed upload size |
| `Media:AllowedExtensions` | `.jpg`, `.png`, `.gif`, `.mp4`, `.mov` | Allowed file extensions |

## Authentication

JWT Bearer token authentication secures all API endpoints except:
- `GET /` (root info)
- `GET /health` (health check)
- `POST /api/auth/register` (registration)
- `POST /api/auth/login` (login)

Tokens are signed with a symmetric key configured in `appsettings.json`.
