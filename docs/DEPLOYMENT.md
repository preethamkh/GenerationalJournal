# Deployment

This guide covers hosting the Generational Journal API and Blazor web app in
the cloud and migrating the database from SQLite to PostgreSQL.

## Hosting overview

The application is a standard .NET 9 app with two deployable components:

- **API** (`GenerationalJournal.Api`) - Minimal API, also serves the Swagger UI.
- **Web** (`GenerationalJournal.Web`) - Blazor Web App using Interactive Server
  render mode, which requires a persistent WebSocket connection between browser
  and server and a process that stays alive for the duration of the session.

Blazor Interactive Server needs sticky sessions (or a single instance) when run
behind a load balancer, because the SignalR circuit is tied to one server
instance. For small/family deployments a single instance is the simplest and
most reliable option.

Media is decoupled from the database through the `IMediaStorage` abstraction
(see [docs/ARCHITECTURE.md](ARCHITECTURE.md#storage-abstraction)). Locally the
default `LocalFileSystemMediaStorage` writes to `data/media/`. In the cloud you
should point media at object storage and either mount it to the container/VM or
implement the `CloudBlobMediaStorage` provider.

## Cloud hosting options

### Azure

| Service | Use |
| --- | --- |
| App Service (Linux, .NET 9) | Run the API and web app on a PaaS host |
| Azure Blob Storage | Media files and database backups |
| Azure Database for PostgreSQL | Production database after migration |
| Azure Functions | Existing thumbnail/backup/health functions |

Steps:

1. Create an App Service plan and web app for the API (`dotnet publish` the API
   project and deploy via the `Azure/webapps-deploy` action or `az webapp deploy`).
2. Create a Storage account; set `Media:StorageProvider=CloudBlob` (once
   `CloudBlobMediaStorage` is implemented) or mount a file share as the
   `Media:StorageRootPath`.
3. Point `ConnectionStrings:DefaultConnection` at Azure Database for
   PostgreSQL after migrating (see below).
4. Deploy the Functions app using `deploy/azuredeploy.bicep` and the
   `.github/workflows/deploy-functions.yml` workflow.

### Oracle Cloud Free Tier

Oracle's Always Free tier includes 2 AMD VMs and up to 4 ARM (Ampere) VMs with
24 GB RAM total, plus 200 GB of block volume - enough for a small production
deployment at no cost.

Steps:

1. Create an Always Free ARM/AMD VM with Ubuntu.
2. Install the .NET 9 runtime (`dotnet-install.sh` or the Microsoft apt
   repository).
3. Publish the API and web app, run them behind a reverse proxy such as Nginx
   (which also terminates TLS and enables sticky sessions).
4. Store SQLite on the VM's boot/block volume or migrate to PostgreSQL (see
   below). Back up the `data/` directory to Object Storage or an external
   location using the scheduled backup workflow or a cron `rsync`.
5. (Optional) Oracle also offers a free Oracle Database / PostgreSQL service
   that can replace SQLite.

### Backblaze B2

B2 is low-cost object storage, well suited as a media store and off-site backup
target rather than an application host.

- Use B2 to store database backups and media snapshots; S3-compatible tooling
  (`rclone`, `restic`, or the S3 API) can sync the `data/` directory.
- Implement `CloudBlobMediaStorage` against the S3-compatible API to serve media
  directly from B2.
- Host the app itself on a small VM or the Oracle/Azure free tiers.

### AWS

| Service | Use |
| --- | --- |
| Lightsail / EC2 | Low-cost VM to run the API and web app |
| S3 | Media files and backups |
| RDS for PostgreSQL | Managed PostgreSQL database |

Steps:

1. Launch a Lightsail instance or EC2 instance with Ubuntu.
2. Install the .NET 9 runtime and deploy the published app behind Nginx.
3. Create an S3 bucket for media and backups; sync with `rclone` or implement
   the cloud storage provider.
4. Point the app at RDS PostgreSQL after migration.

## Migrating from SQLite to PostgreSQL

The application currently uses SQLite via EF Core with `EnsureCreated()` (no
migrations). Migrating to PostgreSQL involves switching the EF Core provider,
introducing migrations, and moving the data.

### 1. Add the PostgreSQL provider

Add the `Npgsql.EntityFrameworkCore.PostgreSQL` package to
`GenerationalJournal.Infrastructure` (matching the EF Core version 9.x):

```bash
dotnet add GenerationalJournal.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
```

### 2. Switch the provider

In `GenerationalJournal.Api/Program.cs`, replace the SQLite registration:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));
```

with:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
```

### 3. Update the connection string

Replace the SQLite connection string with a PostgreSQL one in
`appsettings.json` (or an environment variable / secret in production):

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=generationaljournal;Username=journal;Password=..."
}
```

### 4. Introduce EF Core migrations

Replace the `EnsureCreated()` call at the end of `Program.cs` with migrations:

```csharp
db.Database.Migrate();
```

Generate the initial migration (from the `GenerationalJournal.Infrastructure`
directory, with the design-time package installed):

```bash
dotnet tool install --global dotnet-ef
dotnet add GenerationalJournal.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet ef migrations add InitialCreate --project GenerationalJournal.Infrastructure \
  --startup-project GenerationalJournal.Api
```

Apply it to the new database:

```bash
dotnet ef database update --project GenerationalJournal.Infrastructure \
  --startup-project GenerationalJournal.Api
```

### 5. Move the data

The schema must be created in PostgreSQL first (step 4). Then copy the rows from
SQLite. Options:

- **EF Core / scripting**: Write a small one-off program that reads entities
  from the SQLite `AppDbContext` and inserts them into the PostgreSQL
  `AppDbContext` (preserving IDs).
- **SQL dump/load**: Export each SQLite table to CSV and `COPY` into
  PostgreSQL, taking care to translate types and re-create GUID columns.

Because entity IDs are GUIDs, a straightforward row copy preserves foreign keys
without remapping.

### 6. Update the backup function

`GenerationalJournal.Functions/DatabaseBackupFunction.cs` uses
`Microsoft.Data.Sqlite` and `VACUUM INTO`, which are SQLite-specific. After
migration, replace it with a PostgreSQL dump (e.g. `pg_dump`) that uploads the
result to the backup blob container.

### 7. Move media

Media already lives behind `IMediaStorage`, so the database rows are unaffected
by the provider switch. Copy the `data/media/` tree to the cloud store and set
`Media:StorageRootPath` (or implement `CloudBlobMediaStorage`) to point at the
new location.

## Security checklist

- Store secrets (JWT key, connection strings) in environment variables or a
  secret manager, never in source control.
- Enforce HTTPS (TLS) in production and terminate it at the reverse proxy.
- Restrict CORS (`Media`/API `AllowedHosts` and the `AllowAll` policy) before
  going to production.
- Rotate the JWT signing key and use a strong, random value.
