namespace GenerationalJournal.Application.Services;

using GenerationalJournal.Application.DTOs.Journal;

public interface IJournalService
{
    Task<JournalEntryResponse> CreateEntryAsync(Guid familyId, CreateJournalEntryRequest request, Guid userId);
    Task<JournalEntryResponse> UpdateEntryAsync(Guid entryId, UpdateJournalEntryRequest request, Guid userId);
    Task DeleteEntryAsync(Guid entryId, Guid userId);
    Task<JournalEntryResponse> GetEntryByIdAsync(Guid entryId, Guid userId);
    Task<JournalEntryListResponse> GetEntriesByFamilyAsync(
        Guid familyId, Guid userId, int page, int pageSize, DateTime? fromDate, DateTime? toDate, string? mood);
    Task<List<JournalEntryResponse>> GetEntriesByAuthorAsync(Guid authorId);
    Task<JournalEntryListResponse> SearchEntriesAsync(string query, Guid userId, int page, int pageSize);
}
