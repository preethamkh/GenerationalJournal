using GenerationalJournal.Domain.Entities;

namespace GenerationalJournal.Domain.Repositories;

public interface IMediaRepository
{
    Task<MediaItem?> GetByIdAsync(Guid id);
    Task<List<MediaItem>> GetByEntryIdAsync(Guid entryId);
    Task<MediaItem> CreateAsync(MediaItem mediaItem);
    Task DeleteAsync(MediaItem mediaItem);
}
