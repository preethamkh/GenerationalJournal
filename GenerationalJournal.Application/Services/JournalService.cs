namespace GenerationalJournal.Application.Services;

using GenerationalJournal.Application.DTOs.Journal;
using GenerationalJournal.Domain.Entities;
using GenerationalJournal.Domain.Repositories;

public class JournalService : IJournalService
{
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IFamilyRepository _familyRepository;
    private readonly IUserRepository _userRepository;

    public JournalService(
        IJournalEntryRepository journalEntryRepository,
        IFamilyRepository familyRepository,
        IUserRepository userRepository)
    {
        _journalEntryRepository = journalEntryRepository;
        _familyRepository = familyRepository;
        _userRepository = userRepository;
    }

    public async Task<JournalEntryResponse> CreateEntryAsync(Guid familyId, CreateJournalEntryRequest request, Guid userId)
    {
        await EnsureMemberAsync(familyId, userId);

        var author = await _userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Author not found.");

        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            AuthorId = userId,
            FamilyId = familyId,
            Title = request.Title,
            Content = request.Content,
            Mood = request.Mood,
            EntryDate = request.EntryDate ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsPrivate = request.IsPrivate,
            Tags = request.Tags
        };

        await _journalEntryRepository.CreateAsync(entry);

        entry.Author = author;

        return MapEntry(entry);
    }

    public async Task<JournalEntryResponse> UpdateEntryAsync(Guid entryId, UpdateJournalEntryRequest request, Guid userId)
    {
        var entry = await _journalEntryRepository.GetByIdAsync(entryId)
            ?? throw new KeyNotFoundException("Journal entry not found.");

        EnsureOwner(entry, userId);

        entry.Title = request.Title;
        entry.Content = request.Content;
        entry.Mood = request.Mood;
        entry.IsPrivate = request.IsPrivate;
        entry.Tags = request.Tags;
        entry.UpdatedAt = DateTime.UtcNow;

        if (request.EntryDate.HasValue)
        {
            entry.EntryDate = request.EntryDate.Value;
        }

        await _journalEntryRepository.UpdateAsync(entry);

        return MapEntry(entry);
    }

    public async Task DeleteEntryAsync(Guid entryId, Guid userId)
    {
        var entry = await _journalEntryRepository.GetByIdAsync(entryId)
            ?? throw new KeyNotFoundException("Journal entry not found.");

        EnsureOwner(entry, userId);

        await _journalEntryRepository.DeleteAsync(entry);
    }

    public async Task<JournalEntryResponse> GetEntryByIdAsync(Guid entryId, Guid userId)
    {
        var entry = await _journalEntryRepository.GetByIdAsync(entryId)
            ?? throw new KeyNotFoundException("Journal entry not found.");

        await EnsureMemberAsync(entry.FamilyId, userId);

        return MapEntry(entry);
    }

    public async Task<JournalEntryListResponse> GetEntriesByFamilyAsync(
        Guid familyId, Guid userId, int page, int pageSize, DateTime? fromDate, DateTime? toDate, string? mood)
    {
        await EnsureMemberAsync(familyId, userId);

        var (entries, totalCount) = await _journalEntryRepository.GetByFamilyIdAsync(
            familyId, page, pageSize, fromDate, toDate, mood);

        return MapList(entries, page, pageSize, totalCount);
    }

    public async Task<List<JournalEntryResponse>> GetEntriesByAuthorAsync(Guid authorId)
    {
        var entries = await _journalEntryRepository.GetByAuthorIdAsync(authorId);
        return entries.Select(MapEntry).ToList();
    }

    public async Task<JournalEntryListResponse> SearchEntriesAsync(string query, Guid userId, int page, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new JournalEntryListResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                TotalPages = 0
            };
        }

        var (entries, totalCount) = await _journalEntryRepository.SearchAsync(userId, query, page, pageSize);

        return MapList(entries, page, pageSize, totalCount);
    }

    private async Task EnsureMemberAsync(Guid familyId, Guid userId)
    {
        if (await _familyRepository.GetByIdAsync(familyId) is null)
        {
            throw new KeyNotFoundException("Family not found.");
        }

        if (await _familyRepository.GetMemberAsync(familyId, userId) is null)
        {
            throw new UnauthorizedAccessException("You are not a member of this family.");
        }
    }

    private static void EnsureOwner(JournalEntry entry, Guid userId)
    {
        if (entry.AuthorId != userId)
        {
            throw new UnauthorizedAccessException("Only the author can modify this entry.");
        }
    }

    private static JournalEntryResponse MapEntry(JournalEntry entry)
    {
        return new JournalEntryResponse
        {
            Id = entry.Id,
            AuthorId = entry.AuthorId,
            FamilyId = entry.FamilyId,
            Title = entry.Title,
            Content = entry.Content,
            Mood = entry.Mood,
            EntryDate = entry.EntryDate,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt,
            IsPrivate = entry.IsPrivate,
            Tags = entry.Tags,
            AuthorFirstName = entry.Author?.FirstName ?? string.Empty,
            AuthorLastName = entry.Author?.LastName ?? string.Empty
        };
    }

    private static JournalEntryListResponse MapList(List<JournalEntry> entries, int page, int pageSize, int totalCount)
    {
        return new JournalEntryListResponse
        {
            Items = entries.Select(MapEntry).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
