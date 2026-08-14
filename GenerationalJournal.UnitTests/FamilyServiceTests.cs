using GenerationalJournal.Application.DTOs.Family;
using GenerationalJournal.Application.Services;
using GenerationalJournal.Domain.Entities;
using GenerationalJournal.Domain.Repositories;
using Moq;

namespace GenerationalJournal.UnitTests;

public class FamilyServiceTests
{
    private readonly Mock<IFamilyRepository> _familyRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly FamilyService _sut;

    public FamilyServiceTests()
    {
        _sut = new FamilyService(_familyRepositoryMock.Object, _userRepositoryMock.Object);
    }

    private static Family CreateFamily(Guid createdByUserId, string name = "Smith Family")
    {
        return new Family
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Our family",
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static FamilyMember CreateAdminMember(Guid familyId, Guid userId)
    {
        return new FamilyMember
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            UserId = userId,
            Role = "Admin",
            JoinedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task CreateFamilyAsync_AddsCreatorAsAdmin()
    {
        var userId = Guid.NewGuid();
        var request = new CreateFamilyRequest { Name = "Smith Family", Description = "Our family" };

        Family? captured = null;
        _familyRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Family>()))
            .Callback<Family>(f => captured = f)
            .ReturnsAsync((Family f) => f);

        var result = await _sut.CreateFamilyAsync(request, userId);

        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(userId, result.CreatedByUserId);
        Assert.NotNull(captured);
        Assert.Single(captured!.Members);
        Assert.Equal(userId, captured.Members.First().UserId);
        Assert.Equal("Admin", captured.Members.First().Role);
    }

    [Fact]
    public async Task GetUserFamiliesAsync_ReturnsMappedFamilies()
    {
        var userId = Guid.NewGuid();
        var family = CreateFamily(userId);

        _familyRepositoryMock.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Family> { family });

        var result = await _sut.GetUserFamiliesAsync(userId);

        Assert.Single(result);
        Assert.Equal(family.Id, result[0].Id);
        Assert.Equal(family.Name, result[0].Name);
    }

    [Fact]
    public async Task GetFamilyByIdAsync_WhenNotMember_ThrowsUnauthorizedAccessException()
    {
        var familyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var family = CreateFamily(userId);
        family.Id = familyId;

        _familyRepositoryMock.Setup(r => r.GetByIdAsync(familyId)).ReturnsAsync(family);
        _familyRepositoryMock.Setup(r => r.GetMemberAsync(familyId, userId))
            .ReturnsAsync((FamilyMember?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.GetFamilyByIdAsync(familyId, userId));
    }

    [Fact]
    public async Task AddMemberAsync_WhenAdmin_AddsMember()
    {
        var familyId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var family = CreateFamily(adminId);
        family.Id = familyId;

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "member@example.com",
            FirstName = "John",
            LastName = "Smith"
        };

        _familyRepositoryMock.Setup(r => r.GetByIdAsync(familyId)).ReturnsAsync(family);
        _familyRepositoryMock.Setup(r => r.GetMemberAsync(familyId, adminId))
            .ReturnsAsync(CreateAdminMember(familyId, adminId));
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(newUser.Email)).ReturnsAsync(newUser);
        _familyRepositoryMock.Setup(r => r.GetMemberAsync(familyId, newUser.Id))
            .ReturnsAsync((FamilyMember?)null);
        _familyRepositoryMock.Setup(r => r.AddMemberAsync(It.IsAny<FamilyMember>()))
            .ReturnsAsync((FamilyMember m) => m);

        var request = new AddMemberRequest { Email = newUser.Email, Role = "Member" };

        var result = await _sut.AddMemberAsync(familyId, request, adminId);

        Assert.NotNull(result);
        Assert.Equal(newUser.Id, result.UserId);
        Assert.Equal(newUser.Email, result.Email);
        Assert.Equal("Member", result.Role);
    }

    [Fact]
    public async Task AddMemberAsync_WhenNotAdmin_ThrowsUnauthorizedAccessException()
    {
        var familyId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var family = CreateFamily(requesterId);
        family.Id = familyId;

        _familyRepositoryMock.Setup(r => r.GetByIdAsync(familyId)).ReturnsAsync(family);
        _familyRepositoryMock.Setup(r => r.GetMemberAsync(familyId, requesterId))
            .ReturnsAsync(new FamilyMember
            {
                Id = Guid.NewGuid(),
                FamilyId = familyId,
                UserId = requesterId,
                Role = "Member"
            });

        var request = new AddMemberRequest { Email = "member@example.com" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.AddMemberAsync(familyId, request, requesterId));
    }

    [Fact]
    public async Task AddMemberAsync_WhenAlreadyMember_ThrowsInvalidOperationException()
    {
        var familyId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var family = CreateFamily(adminId);
        family.Id = familyId;

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "member@example.com",
            FirstName = "John",
            LastName = "Smith"
        };

        _familyRepositoryMock.Setup(r => r.GetByIdAsync(familyId)).ReturnsAsync(family);
        _familyRepositoryMock.Setup(r => r.GetMemberAsync(familyId, adminId))
            .ReturnsAsync(CreateAdminMember(familyId, adminId));
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(existingUser.Email)).ReturnsAsync(existingUser);
        _familyRepositoryMock.Setup(r => r.GetMemberAsync(familyId, existingUser.Id))
            .ReturnsAsync(new FamilyMember { Id = Guid.NewGuid(), FamilyId = familyId, UserId = existingUser.Id });

        var request = new AddMemberRequest { Email = existingUser.Email };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AddMemberAsync(familyId, request, adminId));
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenNotAdmin_ThrowsUnauthorizedAccessException()
    {
        var familyId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var family = CreateFamily(requesterId);
        family.Id = familyId;

        _familyRepositoryMock.Setup(r => r.GetByIdAsync(familyId)).ReturnsAsync(family);
        _familyRepositoryMock.Setup(r => r.GetMemberAsync(familyId, requesterId))
            .ReturnsAsync(new FamilyMember
            {
                Id = Guid.NewGuid(),
                FamilyId = familyId,
                UserId = requesterId,
                Role = "Member"
            });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.RemoveMemberAsync(familyId, Guid.NewGuid(), requesterId));

        _familyRepositoryMock.Verify(r => r.RemoveMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetFamilyMembersAsync_WhenNotMember_ThrowsUnauthorizedAccessException()
    {
        var familyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var family = CreateFamily(userId);
        family.Id = familyId;

        _familyRepositoryMock.Setup(r => r.GetByIdAsync(familyId)).ReturnsAsync(family);
        _familyRepositoryMock.Setup(r => r.GetMemberAsync(familyId, userId))
            .ReturnsAsync((FamilyMember?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.GetFamilyMembersAsync(familyId, userId));
    }
}
