namespace GenerationalJournal.Infrastructure.Storage;

using GenerationalJournal.Application.Storage;

public class CloudBlobMediaStorage : IMediaStorage
{
    public Task<string> SaveAsync(
        string relativePath, Stream content, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Azure Blob storage is not yet implemented.");

    public Task<Stream> GetAsync(string storagePath, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Azure Blob storage is not yet implemented.");

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Azure Blob storage is not yet implemented.");

    public Task BackupAsync(string storagePath, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Azure Blob storage is not yet implemented.");
}
