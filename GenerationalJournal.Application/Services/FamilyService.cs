namespace GenerationalJournal.Application.Services;

using GenerationalJournal.Application.DTOs.Family;
using GenerationalJournal.Domain.Entities;
using GenerationalJournal.Domain.Repositories;

public class FamilyService : IFamilyService
{
    private readonly IFamilyRepository _familyRepository;
    private readonly IUserRepository _userRepository;

    public FamilyService(IFamilyRepository familyRepository, IUserRepository userRepository)
    {
        _familyRepository = familyRepository;
        _userRepository = userRepository;
    }

    public async Task<FamilyResponse> CreateFamilyAsync(CreateFamilyRequest request, Guid userId)
    {
        var family = new Family
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        family.Members.Add(new FamilyMember
        {
            Id = Guid.NewGuid(),
            FamilyId = family.Id,
            UserId = userId,
            Role = "Admin",
            JoinedAt = DateTime.UtcNow,
            RelationshipDescription = string.Empty
        });

        await _familyRepository.CreateAsync(family);

        return MapFamily(family);
    }

    public async Task<List<FamilyResponse>> GetUserFamiliesAsync(Guid userId)
    {
        var families = await _familyRepository.GetByUserIdAsync(userId);
        return families.Select(MapFamily).ToList();
    }

    public async Task<FamilyResponse> GetFamilyByIdAsync(Guid familyId, Guid userId)
    {
        var family = await _familyRepository.GetByIdAsync(familyId)
            ?? throw new KeyNotFoundException("Family not found.");

        await EnsureMemberAsync(familyId, userId);

        return MapFamily(family);
    }

    public async Task<FamilyMemberResponse> AddMemberAsync(Guid familyId, AddMemberRequest request, Guid requesterUserId)
    {
        await EnsureAdminAsync(familyId, requesterUserId);

        var user = await _userRepository.GetByEmailAsync(request.Email)
            ?? throw new KeyNotFoundException("A user with this email was not found.");

        if (await _familyRepository.GetMemberAsync(familyId, user.Id) is not null)
        {
            throw new InvalidOperationException("This user is already a member of the family.");
        }

        var member = new FamilyMember
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            UserId = user.Id,
            Role = string.IsNullOrWhiteSpace(request.Role) ? "Member" : request.Role,
            JoinedAt = DateTime.UtcNow,
            RelationshipDescription = request.RelationshipDescription
        };

        await _familyRepository.AddMemberAsync(member);

        return MapMember(member, user);
    }

    public async Task<List<FamilyMemberResponse>> GetFamilyMembersAsync(Guid familyId, Guid userId)
    {
        await EnsureMemberAsync(familyId, userId);

        var members = await _familyRepository.GetMembersAsync(familyId);
        return members.Select(m => MapMember(m, m.User)).ToList();
    }

    public async Task RemoveMemberAsync(Guid familyId, Guid memberUserId, Guid requesterUserId)
    {
        await EnsureAdminAsync(familyId, requesterUserId);

        if (await _familyRepository.GetMemberAsync(familyId, memberUserId) is null)
        {
            throw new KeyNotFoundException("Member not found.");
        }

        await _familyRepository.RemoveMemberAsync(familyId, memberUserId);
    }

    private async Task EnsureAdminAsync(Guid familyId, Guid userId)
    {
        if (await _familyRepository.GetByIdAsync(familyId) is null)
        {
            throw new KeyNotFoundException("Family not found.");
        }

        var member = await _familyRepository.GetMemberAsync(familyId, userId)
            ?? throw new UnauthorizedAccessException("You are not a member of this family.");

        if (!member.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Only family admins can perform this action.");
        }
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

    private static FamilyResponse MapFamily(Family family)
    {
        return new FamilyResponse
        {
            Id = family.Id,
            Name = family.Name,
            Description = family.Description,
            CreatedByUserId = family.CreatedByUserId,
            CreatedAt = family.CreatedAt
        };
    }

    private static FamilyMemberResponse MapMember(FamilyMember member, User user)
    {
        return new FamilyMemberResponse
        {
            Id = member.Id,
            FamilyId = member.FamilyId,
            UserId = member.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = member.Role,
            JoinedAt = member.JoinedAt,
            RelationshipDescription = member.RelationshipDescription
        };
    }
}
