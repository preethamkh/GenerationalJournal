namespace GenerationalJournal.Infrastructure.Storage;

using GenerationalJournal.Application.Configuration;
using GenerationalJournal.Application.Storage;
using Microsoft.Extensions.Options;

public class LocalFileSystemMediaStorage : IMediaStorage
{
    private readonly string _rootPath;

    public LocalFileSystemMediaStorage(IOptions<MediaSettings> settings)
    {
        _rootPath = settings.Value.StorageRootPath;
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(
        string relativePath, Stream content, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(relativePath);

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);
        await content.CopyToAsync(fileStream, cancellationToken);

        return relativePath;
    }

    public Task<Stream> GetAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath);

        if (!File.Exists(fullPath))
        {
            throw new KeyNotFoundException("Media file not found.");
        }

        return Task.FromResult<Stream>(
            new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task BackupAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Media file not found.", fullPath);
        }

        File.Copy(fullPath, fullPath + ".bak", overwrite: true);

        return Task.CompletedTask;
    }

    private string GetFullPath(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_rootPath, normalized);
    }
}
