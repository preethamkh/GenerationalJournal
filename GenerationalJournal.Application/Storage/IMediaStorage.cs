namespace GenerationalJournal.Application.Storage;

public interface IMediaStorage
{
    Task<string> SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken = default);

    Task<Stream> GetAsync(string storagePath, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);

    Task BackupAsync(string storagePath, CancellationToken cancellationToken = default);
}
