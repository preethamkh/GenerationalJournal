using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenerationalJournal.Functions;

public class DatabaseBackupFunction
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseBackupFunction> _logger;

    public DatabaseBackupFunction(IConfiguration configuration, ILogger<DatabaseBackupFunction> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [Function(nameof(DatabaseBackupFunction))]
    public async Task RunAsync(
        [TimerTrigger("0 0 2 * * *")] TimerInfo timer,
        FunctionContext context)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        var sourcePath = ExtractDataSourcePath(connectionString);
        var backupFileName = $"familyjournal-{DateTime.UtcNow:yyyyMMdd-HHmmss}.db";
        var localBackupPath = Path.Combine(Path.GetTempPath(), backupFileName);

        try
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = "VACUUM INTO $backupPath;";
                command.Parameters.AddWithValue("$backupPath", localBackupPath);
                await command.ExecuteNonQueryAsync();
            }

            await UploadToBlobAsync(localBackupPath, backupFileName);

            _logger.LogInformation(
                "Database backup completed and uploaded to {Container}/{Blob}.",
                ResolveContainerName(), backupFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database backup failed for source {SourcePath}.", sourcePath);
            throw;
        }
        finally
        {
            if (File.Exists(localBackupPath))
            {
                File.Delete(localBackupPath);
            }
        }
    }

    private async Task UploadToBlobAsync(string localPath, string blobName)
    {
        var storageConnection = _configuration["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException("Storage connection 'AzureWebJobsStorage' is not configured.");

        var containerName = ResolveContainerName();

        var blobServiceClient = new BlobServiceClient(storageConnection);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(blobName);
        await using var stream = File.OpenRead(localPath);
        await blobClient.UploadAsync(stream, overwrite: true);
    }

    private string ResolveContainerName() =>
        _configuration["DatabaseBackup:Container"] ?? "database-backups";

    private static string ExtractDataSourcePath(string connectionString)
    {
        foreach (var segment in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = segment.IndexOf('=');
            if (separatorIndex < 0)
            {
                continue;
            }

            var key = segment[..separatorIndex].Trim();
            if (key.Equals("Data Source", StringComparison.OrdinalIgnoreCase))
            {
                return segment[(separatorIndex + 1)..].Trim();
            }
        }

        return connectionString;
    }
}
