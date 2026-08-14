namespace GenerationalJournal.Infrastructure.Repositories;

using GenerationalJournal.Domain.Entities;
using GenerationalJournal.Domain.Repositories;
using GenerationalJournal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class JournalEntryRepository : IJournalEntryRepository
{
    private readonly AppDbContext _context;

    public JournalEntryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<JournalEntry?> GetByIdAsync(Guid id)
    {
        return await _context.JournalEntries
            .Include(e => e.Author)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<(List<JournalEntry> Entries, int TotalCount)> GetByFamilyIdAsync(
        Guid familyId, int page, int pageSize, DateTime? fromDate, DateTime? toDate, string? mood)
    {
        var query = _context.JournalEntries
            .Include(e => e.Author)
            .Where(e => e.FamilyId == familyId);

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.EntryDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(e => e.EntryDate <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(mood))
        {
            query = query.Where(e => e.Mood == mood);
        }

        var totalCount = await query.CountAsync();

        var entries = await query
            .OrderByDescending(e => e.EntryDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (entries, totalCount);
    }

    public async Task<List<JournalEntry>> GetByAuthorIdAsync(Guid authorId)
    {
        return await _context.JournalEntries
            .Include(e => e.Author)
            .Where(e => e.AuthorId == authorId)
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync();
    }

    public async Task<(List<JournalEntry> Entries, int TotalCount)> SearchAsync(
        Guid userId, string query, int page, int pageSize)
    {
        var familyIds = _context.FamilyMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.FamilyId);

        var searchTerm = query.Trim();

        var searchQuery = _context.JournalEntries
            .Include(e => e.Author)
            .Where(e => familyIds.Contains(e.FamilyId))
            .Where(e => e.Title.Contains(searchTerm)
                || e.Content.Contains(searchTerm)
                || e.Tags.Contains(searchTerm));

        var totalCount = await searchQuery.CountAsync();

        var entries = await searchQuery
            .OrderByDescending(e => e.EntryDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (entries, totalCount);
    }

    public async Task<JournalEntry> CreateAsync(JournalEntry entry)
    {
        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
        return entry;
    }

    public async Task<JournalEntry> UpdateAsync(JournalEntry entry)
    {
        _context.JournalEntries.Update(entry);
        await _context.SaveChangesAsync();
        return entry;
    }

    public async Task DeleteAsync(JournalEntry entry)
    {
        _context.JournalEntries.Remove(entry);
        await _context.SaveChangesAsync();
    }
}
