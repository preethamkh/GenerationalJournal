using GenerationalJournal.Domain.Entities;

namespace GenerationalJournal.Domain.Repositories;

public interface IJournalEntryRepository
{
    Task<JournalEntry?> GetByIdAsync(Guid id);
    Task<(List<JournalEntry> Entries, int TotalCount)> GetByFamilyIdAsync(
        Guid familyId, int page, int pageSize, DateTime? fromDate, DateTime? toDate, string? mood);
    Task<List<JournalEntry>> GetByAuthorIdAsync(Guid authorId);
    Task<(List<JournalEntry> Entries, int TotalCount)> SearchAsync(
        Guid userId, string query, int page, int pageSize);
    Task<JournalEntry> CreateAsync(JournalEntry entry);
    Task<JournalEntry> UpdateAsync(JournalEntry entry);
    Task DeleteAsync(JournalEntry entry);
}
