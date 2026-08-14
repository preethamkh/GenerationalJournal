using GenerationalJournal.Application.DTOs.Journal;
using GenerationalJournal.Application.Services;
using GenerationalJournal.Domain.Entities;
using GenerationalJournal.Domain.Repositories;
using Moq;

namespace GenerationalJournal.UnitTests;

public class JournalServiceTests
{
    private readonly Mock<IJournalEntryRepository> _journalEntryRepositoryMock = new();
    private readonly Mock<IFamilyRepository> _familyRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly JournalService _sut;

    public JournalServiceTests()
    {
        _sut = new JournalService(
            _journalEntryRepositoryMock.Object,
            _familyRepositoryMock.Object,
            _userRepositoryMock.Object);
    }

    private static User CreateUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = "author@example.com",
            FirstName = "Jane",
            LastName = "Doe"
        };
    }

    private static Family CreateFamily(Guid userId)
    {
        return new Family
        {
            Id = Guid.NewGuid(),
            Name = "Smith Family",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static FamilyMember CreateMember(Guid familyId, Guid userId)
    {
        return new FamilyMember
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            UserId = userId,
            Role = "Member",
            JoinedAt = DateTime.UtcNow
        };
    }

    private static JournalEntry CreateEntry(Guid authorId, Guid familyId)
    {
        return new JournalEntry
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            FamilyId = familyId,
            Title = "My Entry",
            Content = "Content",
            Mood = "Happy",
            EntryDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private void SetupMember(Guid familyId, Guid userId)
    {
        _familyRepositoryMock.Setup(r => r.GetByIdAsync(familyId)).ReturnsAsync(CreateFamily(userId));
        _familyRepositoryMock.Setup(r => r.GetMemberAsync(familyId, userId))
            .ReturnsAsync(CreateMember(familyId, userId));
    }

    [Fact]
    public async Task CreateEntryAsync_WhenMember_CreatesEntry()
    {
        var familyId = Guid.NewGuid();
        var user = CreateUser();
        SetupMember(familyId, user.Id);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var request = new CreateJournalEntryRequest
        {
            Title = "My Entry",
            Content = "Content",
            Mood = "Happy",
            IsPrivate = false,
            Tags = "vacation,family"
        };

        var result = await _sut.CreateEntryAsync(familyId, request, user.Id);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.AuthorId);
        Assert.Equal(familyId, result.FamilyId);
        Assert.Equal(request.Title, result.Title);
        Assert.Equal(user.FirstName, result.AuthorFirstName);
        Assert.Equal(user.LastName, result.AuthorLastName);

        _journalEntryRepositoryMock.Verify(r => r.CreateAsync(It.Is<JournalEntry>(e =>
            e.AuthorId == user.Id && e.FamilyId == familyId && e.Title == request.Title)), Times.Once);
    }

    [Fact]
    public async Task CreateEntryAsync_WhenNotMember_ThrowsUnauthorizedAccessException()
    {
        var familyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _familyRepositoryMock.Setup(r => r.GetByIdAsync(familyId)).ReturnsAsync(CreateFamily(userId));
        _familyRepositoryMock.Setup(r => r.GetMemberAsync(familyId, userId))
            .ReturnsAsync((FamilyMember?)null);

        var request = new CreateJournalEntryRequest { Title = "My Entry", Content = "Content" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.CreateEntryAsync(familyId, request, userId));

        _journalEntryRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<JournalEntry>()), Times.Never);
    }

    [Fact]
    public async Task UpdateEntryAsync_WhenOwner_UpdatesEntry()
    {
        var familyId = Guid.NewGuid();
        var user = CreateUser();
        var entry = CreateEntry(user.Id, familyId);

        _journalEntryRepositoryMock.Setup(r => r.GetByIdAsync(entry.Id)).ReturnsAsync(entry);
        _journalEntryRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<JournalEntry>()))
            .ReturnsAsync((JournalEntry e) => e);

        var request = new UpdateJournalEntryRequest
        {
            Title = "Updated Title",
            Content = "Updated Content",
            Mood = "Sad",
            IsPrivate = true,
            Tags = "updated"
        };

        var result = await _sut.UpdateEntryAsync(entry.Id, request, user.Id);

        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        Assert.Equal("Updated Content", result.Content);
        Assert.True(result.IsPrivate);

        _journalEntryRepositoryMock.Verify(r => r.UpdateAsync(It.Is<JournalEntry>(e =>
            e.Id == entry.Id && e.Title == "Updated Title")), Times.Once);
    }

    [Fact]
    public async Task UpdateEntryAsync_WhenNotOwner_ThrowsUnauthorizedAccessException()
    {
        var familyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var entry = CreateEntry(ownerId, familyId);

        _journalEntryRepositoryMock.Setup(r => r.GetByIdAsync(entry.Id)).ReturnsAsync(entry);

        var request = new UpdateJournalEntryRequest { Title = "Hacked", Content = "Hacked" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.UpdateEntryAsync(entry.Id, request, otherUserId));

        _journalEntryRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<JournalEntry>()), Times.Never);
    }

    [Fact]
    public async Task DeleteEntryAsync_WhenOwner_DeletesEntry()
    {
        var familyId = Guid.NewGuid();
        var user = CreateUser();
        var entry = CreateEntry(user.Id, familyId);

        _journalEntryRepositoryMock.Setup(r => r.GetByIdAsync(entry.Id)).ReturnsAsync(entry);

        await _sut.DeleteEntryAsync(entry.Id, user.Id);

        _journalEntryRepositoryMock.Verify(r => r.DeleteAsync(It.Is<JournalEntry>(e => e.Id == entry.Id)), Times.Once);
    }

    [Fact]
    public async Task DeleteEntryAsync_WhenNotOwner_ThrowsUnauthorizedAccessException()
    {
        var familyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var entry = CreateEntry(ownerId, familyId);

        _journalEntryRepositoryMock.Setup(r => r.GetByIdAsync(entry.Id)).ReturnsAsync(entry);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.DeleteEntryAsync(entry.Id, otherUserId));

        _journalEntryRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<JournalEntry>()), Times.Never);
    }

    [Fact]
    public async Task GetEntryByIdAsync_WhenNotMember_ThrowsUnauthorizedAccessException()
    {
        var familyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var entry = CreateEntry(ownerId, familyId);

        _journalEntryRepositoryMock.Setup(r => r.GetByIdAsync(entry.Id)).ReturnsAsync(entry);
        _familyRepositoryMock.Setup(r => r.GetByIdAsync(familyId)).ReturnsAsync(CreateFamily(ownerId));
        _familyRepositoryMock.Setup(r => r.GetMemberAsync(familyId, userId))
            .ReturnsAsync((FamilyMember?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.GetEntryByIdAsync(entry.Id, userId));
    }

    [Fact]
    public async Task GetEntriesByFamilyAsync_ReturnsPaginatedList()
    {
        var familyId = Guid.NewGuid();
        var user = CreateUser();
        SetupMember(familyId, user.Id);

        var entries = new List<JournalEntry>
        {
            CreateEntry(user.Id, familyId),
            CreateEntry(user.Id, familyId)
        };

        _journalEntryRepositoryMock
            .Setup(r => r.GetByFamilyIdAsync(familyId, 1, 20, null, null, null))
            .ReturnsAsync((entries, 2));

        var result = await _sut.GetEntriesByFamilyAsync(familyId, user.Id, 1, 20, null, null, null);

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task SearchEntriesAsync_WithEmptyQuery_ReturnsEmptyResult()
    {
        var result = await _sut.SearchEntriesAsync("   ", Guid.NewGuid(), 1, 20);

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);

        _journalEntryRepositoryMock.Verify(r => r.SearchAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }
}
