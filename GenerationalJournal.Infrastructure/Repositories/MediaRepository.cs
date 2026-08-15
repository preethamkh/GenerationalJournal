namespace GenerationalJournal.Infrastructure.Repositories;

using GenerationalJournal.Domain.Entities;
using GenerationalJournal.Domain.Repositories;
using GenerationalJournal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class MediaRepository : IMediaRepository
{
    private readonly AppDbContext _context;

    public MediaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<MediaItem?> GetByIdAsync(Guid id)
    {
        return await _context.MediaItems
            .Include(m => m.Entry)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<List<MediaItem>> GetByEntryIdAsync(Guid entryId)
    {
        return await _context.MediaItems
            .Where(m => m.EntryId == entryId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<MediaItem> CreateAsync(MediaItem mediaItem)
    {
        _context.MediaItems.Add(mediaItem);
        await _context.SaveChangesAsync();
        return mediaItem;
    }

    public async Task DeleteAsync(MediaItem mediaItem)
    {
        _context.MediaItems.Remove(mediaItem);
        await _context.SaveChangesAsync();
    }
}
