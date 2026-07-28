using GenerationalJournal.Domain.Entities;

namespace GenerationalJournal.Domain.Repositories;

public interface IFamilyRepository
{
    Task<Family?> GetByIdAsync(Guid id);
    Task<List<Family>> GetByUserIdAsync(Guid userId);
    Task<Family> CreateAsync(Family family);
    Task<Family> UpdateAsync(Family family);
    Task DeleteAsync(Family family);
    Task<FamilyMember> AddMemberAsync(FamilyMember member);
    Task RemoveMemberAsync(Guid familyId, Guid userId);
    Task<List<FamilyMember>> GetMembersAsync(Guid familyId);
    Task<FamilyMember?> GetMemberAsync(Guid familyId, Guid userId);
}
